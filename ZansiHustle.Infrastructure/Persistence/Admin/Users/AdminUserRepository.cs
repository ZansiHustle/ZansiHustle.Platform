using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Admin.Users.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Persistence.Admin.Users;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Queries;

namespace ZansiHustle.Infrastructure.Persistence.Admin.Users
{
    /// <summary>
    /// EF Core-backed queries for the admin Users view. Designed to avoid
    /// N+1: for any page size, exactly three round-trips hit the DB —
    /// count + paged users + one batched roles query + one batched
    /// merchants query. All filters (userType / merchantType / role /
    /// search / status) are applied in SQL before pagination.
    /// </summary>
    public sealed class AdminUserRepository : IAdminUserRepository
    {
        private readonly AppDbContext _context;

        public AdminUserRepository(AppDbContext context)
        {
            _context = context;
        }

        // ── Role-name constants (single source of truth for classification) ──
        // Role names are stored in AspNetRoles as the enum member names
        // (see IdentitySeeder). Keeping them centralised here so the
        // userType derivation below and the filter predicates agree.

        private static readonly string[] AdminRoleNames = new[]
        {
            nameof(UserRole.SuperAdmin),
            nameof(UserRole.Admin),
            nameof(UserRole.Partner),
            nameof(UserRole.MarketingManager),
            nameof(UserRole.TeamManager),
            nameof(UserRole.Accountant),
            nameof(UserRole.Moderator),
            nameof(UserRole.Support),
        };

        private static readonly string[] TeamRoleNames = new[]
        {
            nameof(UserRole.MarketplaceGrowthAssociate),
            nameof(UserRole.SocialMediaManager),
            nameof(UserRole.ContentCreator),
            nameof(UserRole.TeamMember),
        };

        private const string AgentRoleName = nameof(UserRole.Agent);

        private static readonly string[] MerchantRoleNames = new[]
        {
            nameof(UserRole.Merchant),
            nameof(UserRole.Seller),
            nameof(UserRole.ServiceProvider),
            nameof(UserRole.BusinessOwner),
            nameof(UserRole.MarketplaceSeller),
        };

        /// <summary>
        /// Role names that "elevate" a user out of the Buyer bucket.
        /// Used for the Buyer filter (exclude anyone in these roles)
        /// and for classifying UserType.
        /// </summary>
        private static readonly string[] ElevatedRoleNames =
            AdminRoleNames.Concat(TeamRoleNames).Append(AgentRoleName).Concat(MerchantRoleNames).ToArray();

        // ── Paged list ─────────────────────────────────────────────

        public async Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQueryRequestDto query)
        {
            query ??= new UserQueryRequestDto();
            var baseQuery = _context.Users.AsNoTracking().AsQueryable();

            // 1. Search — name / email / phone. We use EF.Functions.Like
            //    so the matching is server-side and index-friendly.
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim();
                var like = $"%{s}%";
                baseQuery = baseQuery.Where(u =>
                    EF.Functions.Like(u.FirstName, like)
                    || EF.Functions.Like(u.LastName, like)
                    || EF.Functions.Like((u.FirstName + " " + u.LastName), like)
                    || (u.Email != null && EF.Functions.Like(u.Email, like))
                    || (u.PhoneNumber != null && EF.Functions.Like(u.PhoneNumber, like)));
            }

            // 2. Status filter (string: active / suspended / inactive).
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                if (TryParseAccountStatus(query.Status, out var status))
                    baseQuery = baseQuery.Where(u => u.AccountStatus == status);
            }

            // 3. UserType filter — translate to a role-membership or
            //    merchant-existence predicate. Each branch produces a
            //    SQL-translatable `Where` that runs before pagination.
            var userType = Normalise(query.UserType);

            if (string.Equals(userType, UserTypeCodes.Admin, StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = FilterByAnyRole(baseQuery, AdminRoleNames);
            }
            else if (string.Equals(userType, UserTypeCodes.TeamMember, StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = FilterByAnyRole(baseQuery, TeamRoleNames);
            }
            else if (string.Equals(userType, UserTypeCodes.Agent, StringComparison.OrdinalIgnoreCase))
            {
                baseQuery = FilterByAnyRole(baseQuery, new[] { AgentRoleName });
            }
            else if (string.Equals(userType, UserTypeCodes.Merchant, StringComparison.OrdinalIgnoreCase))
            {
                // Merchant = has a Merchants row OR a merchant-family role.
                // We union both conditions so legacy users with only a role
                // are still surfaced alongside those with a real Merchant
                // record.
                var merchantOwnerIds = _context.Merchants
                    .Where(m => m.OwnerUserId.HasValue)
                    .Select(m => m.OwnerUserId!.Value);

                var merchantRoleUserIds = from ur in _context.UserRoles
                                          join r in _context.Roles on ur.RoleId equals r.Id
                                          where MerchantRoleNames.Contains(r.Name!)
                                          select ur.UserId;

                baseQuery = baseQuery.Where(u =>
                    merchantOwnerIds.Contains(u.Id) || merchantRoleUserIds.Contains(u.Id));

                // Optional merchant subtype filter — narrow further by role / raw type.
                baseQuery = ApplyMerchantSubTypeFilter(baseQuery, query.MerchantType);
            }
            else if (string.Equals(userType, UserTypeCodes.Buyer, StringComparison.OrdinalIgnoreCase))
            {
                // Buyer = authenticated user with NO elevated role AND NO merchant record.
                var elevatedUserIds = from ur in _context.UserRoles
                                      join r in _context.Roles on ur.RoleId equals r.Id
                                      where ElevatedRoleNames.Contains(r.Name!)
                                      select ur.UserId;
                var merchantOwnerIds = _context.Merchants
                    .Where(m => m.OwnerUserId.HasValue)
                    .Select(m => m.OwnerUserId!.Value);

                baseQuery = baseQuery.Where(u =>
                    !elevatedUserIds.Contains(u.Id) && !merchantOwnerIds.Contains(u.Id));
            }

            // 4. Explicit role filter (e.g. role=SuperAdmin). Additive on
            //    top of userType so a caller can say "Admin users who are Partners".
            if (!string.IsNullOrWhiteSpace(query.Role))
            {
                var roleName = query.Role.Trim();
                baseQuery = FilterByAnyRole(baseQuery, new[] { roleName });
            }

            // 5. Count BEFORE paging so the client can render page controls.
            var total = await baseQuery.CountAsync();

            // 6. Page the users. Deterministic tiebreaker on Id keeps pages stable
            //    when many users share the same CreatedOnUtc (batch seeds).
            var pagedUsers = await baseQuery
                .OrderByDescending(u => u.CreatedOnUtc)
                .ThenBy(u => u.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            // 7. Batched roles lookup for just this page of users.
            var userIds = pagedUsers.Select(u => u.Id).ToList();
            var rolesByUser = await LoadRolesAsync(userIds);

            // 8. Batched merchants lookup.
            var merchantByUser = await LoadMerchantsAsync(userIds);

            // 9. Compose DTOs in memory — no further DB hits.
            var items = pagedUsers
                .Select(u => ToDto(u,
                                   rolesByUser.TryGetValue(u.Id, out var rs) ? rs : new List<string>(),
                                   merchantByUser.TryGetValue(u.Id, out var m) ? m : null))
                .ToList();

            return new PagedResult<UserListItemDto>
            {
                Items = items,
                Total = total,
                Page = query.Page,
                PageSize = query.PageSize,
            };
        }

        // ── Single user ────────────────────────────────────────────

        public async Task<UserListItemDto?> GetByIdAsync(Guid id)
        {
            var user = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return null;

            var ids = new List<Guid> { id };
            var rolesByUser = await LoadRolesAsync(ids);
            var merchantByUser = await LoadMerchantsAsync(ids);

            return ToDto(user,
                         rolesByUser.TryGetValue(id, out var rs) ? rs : new List<string>(),
                         merchantByUser.TryGetValue(id, out var m) ? m : null);
        }

        // ── KPIs ───────────────────────────────────────────────────

        public async Task<UsersKpisDto> GetKpisAsync()
        {
            // One SQL round-trip per metric — none of these loop in-memory.
            // Buyer count is computed from the known totals rather than as
            // its own query so we don't have to NOT-IN 20+ role names on
            // the DB (cheaper, simpler, deterministic).

            var totalUsers = await _context.Users.AsNoTracking().CountAsync();
            var activeUsers = await _context.Users.AsNoTracking()
                .CountAsync(u => u.AccountStatus == AccountStatus.Active);

            // Distinct-user counts per bucket. Some users hold multiple
            // roles (e.g. Admin + Partner) — Distinct() collapses to a
            // single count rather than double-counting.
            var adminCount = await CountUsersInAnyRoleAsync(AdminRoleNames);
            var teamCount  = await CountUsersInAnyRoleAsync(TeamRoleNames);
            var agentCount = await CountUsersInAnyRoleAsync(new[] { AgentRoleName });

            // Merchant count = distinct users who either own a Merchants
            // row or hold a merchant-family role. Matches the UserType
            // derivation exactly.
            var merchantOwnerIds = _context.Merchants
                .Where(m => m.OwnerUserId.HasValue)
                .Select(m => m.OwnerUserId!.Value);
            var merchantRoleUserIds = from ur in _context.UserRoles
                                      join r in _context.Roles on ur.RoleId equals r.Id
                                      where MerchantRoleNames.Contains(r.Name!)
                                      select ur.UserId;
            var merchantCount = await _context.Users.AsNoTracking()
                .Where(u => merchantOwnerIds.Contains(u.Id) || merchantRoleUserIds.Contains(u.Id))
                .Select(u => u.Id).Distinct()
                .CountAsync();

            // Buyers = users with NO elevated role AND NO merchant record.
            // Derive as "total minus the distinct elevated set" to keep this
            // query cheap and avoid a large NOT-IN expression.
            var elevatedUserIds = from ur in _context.UserRoles
                                  join r in _context.Roles on ur.RoleId equals r.Id
                                  where ElevatedRoleNames.Contains(r.Name!)
                                  select ur.UserId;
            var classifiedCount = await _context.Users.AsNoTracking()
                .Where(u => elevatedUserIds.Contains(u.Id) || merchantOwnerIds.Contains(u.Id))
                .Select(u => u.Id).Distinct()
                .CountAsync();
            var buyerCount = totalUsers - classifiedCount;
            if (buyerCount < 0) buyerCount = 0; // defensive — shouldn't happen

            return new UsersKpisDto
            {
                TotalUsers   = totalUsers,
                ActiveUsers  = activeUsers,
                Admins       = adminCount,
                TeamMembers  = teamCount,
                Agents       = agentCount,
                Merchants    = merchantCount,
                Buyers       = buyerCount,
            };
        }

        // ── Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Narrow a user query to rows that hold at least one of the
        /// supplied role names. Uses a sub-query rather than a join so
        /// the outer query shape is preserved (still a plain User query).
        /// </summary>
        private IQueryable<Domain.Identity.User> FilterByAnyRole(IQueryable<Domain.Identity.User> users, string[] roleNames)
        {
            var userIdsInRoles = from ur in _context.UserRoles
                                 join r in _context.Roles on ur.RoleId equals r.Id
                                 where roleNames.Contains(r.Name!)
                                 select ur.UserId;
            return users.Where(u => userIdsInRoles.Contains(u.Id));
        }

        /// <summary>
        /// Narrow a Merchant-filtered user query by subtype. Role-first
        /// (ServiceProvider/BusinessOwner are discriminative) then falls
        /// back to Merchant.Type for users with only a Seller/
        /// MarketplaceSeller role. Matches ClassifyMerchantSubType so
        /// a filter value round-trips predictably.
        /// </summary>
        private IQueryable<Domain.Identity.User> ApplyMerchantSubTypeFilter(IQueryable<Domain.Identity.User> users, string? subtype)
        {
            var code = Normalise(subtype);
            if (string.IsNullOrWhiteSpace(code)) return users;

            if (string.Equals(code, MerchantSubTypeCodes.ServiceProvider, StringComparison.OrdinalIgnoreCase))
            {
                return FilterByAnyRole(users, new[] { nameof(UserRole.ServiceProvider) });
            }

            if (string.Equals(code, MerchantSubTypeCodes.StoreOwner, StringComparison.OrdinalIgnoreCase))
            {
                var hasBusinessOwnerRole = from ur in _context.UserRoles
                                           join r in _context.Roles on ur.RoleId equals r.Id
                                           where r.Name == nameof(UserRole.BusinessOwner)
                                           select ur.UserId;
                var physicalMerchantOwners = _context.Merchants
                    .Where(m => m.OwnerUserId.HasValue && m.Type == MerchantType.PhysicalStore)
                    .Select(m => m.OwnerUserId!.Value);
                return users.Where(u => hasBusinessOwnerRole.Contains(u.Id) || physicalMerchantOwners.Contains(u.Id));
            }

            if (string.Equals(code, MerchantSubTypeCodes.Seller, StringComparison.OrdinalIgnoreCase))
            {
                var sellerRoleUsers = from ur in _context.UserRoles
                                      join r in _context.Roles on ur.RoleId equals r.Id
                                      where r.Name == nameof(UserRole.Seller)
                                         || r.Name == nameof(UserRole.MarketplaceSeller)
                                         || r.Name == nameof(UserRole.Merchant)
                                      select ur.UserId;
                var onlineMerchantOwners = _context.Merchants
                    .Where(m => m.OwnerUserId.HasValue && m.Type == MerchantType.OnlineStore)
                    .Select(m => m.OwnerUserId!.Value);
                return users.Where(u => sellerRoleUsers.Contains(u.Id) || onlineMerchantOwners.Contains(u.Id));
            }

            // Unknown subtype — ignore rather than crash the query.
            return users;
        }

        private async Task<Dictionary<Guid, List<string>>> LoadRolesAsync(List<Guid> userIds)
        {
            if (userIds.Count == 0) return new Dictionary<Guid, List<string>>();

            var pairs = await (from ur in _context.UserRoles.AsNoTracking()
                               join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                               where userIds.Contains(ur.UserId)
                               select new { ur.UserId, RoleName = r.Name ?? string.Empty })
                              .ToListAsync();

            return pairs
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList());
        }

        /// <summary>
        /// Merchant snapshot for a batch of user ids. A user may own
        /// more than one Merchant; we pick the most recent one for the
        /// summary view — callers needing the full list should use the
        /// merchant-specific endpoints.
        /// </summary>
        private async Task<Dictionary<Guid, MerchantSummary>> LoadMerchantsAsync(List<Guid> userIds)
        {
            if (userIds.Count == 0) return new Dictionary<Guid, MerchantSummary>();

            var rows = await _context.Merchants.AsNoTracking()
                .Where(m => m.OwnerUserId.HasValue && userIds.Contains(m.OwnerUserId!.Value))
                .Select(m => new MerchantSummary
                {
                    Id         = m.Id,
                    OwnerUserId = m.OwnerUserId!.Value,
                    Name       = m.Name,
                    Type       = m.Type,
                    Status     = m.Status,
                    KycStatus  = m.KycStatus,
                    CreatedAtUtc = m.CreatedAtUtc,
                })
                .ToListAsync();

            return rows
                .GroupBy(r => r.OwnerUserId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedAtUtc).First());
        }

        private async Task<int> CountUsersInAnyRoleAsync(string[] roleNames)
        {
            return await (from ur in _context.UserRoles.AsNoTracking()
                          join r in _context.Roles.AsNoTracking() on ur.RoleId equals r.Id
                          where roleNames.Contains(r.Name!)
                          select ur.UserId).Distinct().CountAsync();
        }

        // ── Mapping ────────────────────────────────────────────────

        private static UserListItemDto ToDto(Domain.Identity.User user, List<string> roles, MerchantSummary? merchant)
        {
            var fullName = string.IsNullOrWhiteSpace(user.LastName)
                ? user.FirstName
                : $"{user.FirstName} {user.LastName}".Trim();

            var userType = ClassifyUserType(roles, merchant is not null);
            var merchantSubType = userType == UserTypeCodes.Merchant
                ? ClassifyMerchantSubType(roles, merchant?.Type)
                : null;

            return new UserListItemDto
            {
                Id            = user.Id,
                FullName      = fullName,
                Email         = user.Email ?? string.Empty,
                PhoneNumber   = user.PhoneNumber,
                IsActive      = user.IsActive,
                AccountStatus = user.AccountStatus.ToString(),
                CreatedOnUtc  = user.CreatedOnUtc,
                UpdatedOnUtc  = user.UpdatedOnUtc,
                Roles         = roles,
                UserType      = userType,
                MerchantType  = merchantSubType,
                HasMerchantRecord = merchant is not null,
                MerchantId    = merchant?.Id,
                BusinessName  = merchant?.Name,
                MerchantStatus = merchant?.Status.ToString(),
                MerchantKycStatus = merchant?.KycStatus.ToString(),
                MerchantRawType = merchant?.Type.ToString(),
            };
        }

        private static string ClassifyUserType(IReadOnlyCollection<string> roles, bool hasMerchantRecord)
        {
            // Highest privilege wins: an Admin who also happens to own a
            // merchant is still classified as Admin. Order matters.
            if (roles.Any(r => AdminRoleNames.Contains(r))) return UserTypeCodes.Admin;
            if (roles.Any(r => TeamRoleNames.Contains(r)))  return UserTypeCodes.TeamMember;
            if (roles.Contains(AgentRoleName))              return UserTypeCodes.Agent;

            if (hasMerchantRecord || roles.Any(r => MerchantRoleNames.Contains(r)))
                return UserTypeCodes.Merchant;

            return UserTypeCodes.Buyer;
        }

        private static string? ClassifyMerchantSubType(IReadOnlyCollection<string> roles, MerchantType? merchantType)
        {
            // Role-first: ServiceProvider and BusinessOwner are
            // discriminative (the merchant table only has Physical/Online).
            if (roles.Contains(nameof(UserRole.ServiceProvider))) return MerchantSubTypeCodes.ServiceProvider;
            if (roles.Contains(nameof(UserRole.BusinessOwner)))   return MerchantSubTypeCodes.StoreOwner;

            // Fall back to the merchant record's type.
            if (merchantType == MerchantType.PhysicalStore) return MerchantSubTypeCodes.StoreOwner;
            if (merchantType == MerchantType.OnlineStore)   return MerchantSubTypeCodes.Seller;

            // Role-only fallback for legacy merchants with no Merchants row.
            if (roles.Contains(nameof(UserRole.Seller)))            return MerchantSubTypeCodes.Seller;
            if (roles.Contains(nameof(UserRole.MarketplaceSeller))) return MerchantSubTypeCodes.Seller;
            if (roles.Contains(nameof(UserRole.Merchant)))          return MerchantSubTypeCodes.Seller;

            return null;
        }

        private static bool TryParseAccountStatus(string raw, out AccountStatus status)
        {
            return Enum.TryParse(raw, ignoreCase: true, out status);
        }

        private static string? Normalise(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        /// <summary>
        /// Lightweight projection of <c>Merchant</c> used when joining
        /// into the users list. Keeps the service layer from having to
        /// reference the Merchant entity directly.
        /// </summary>
        private sealed class MerchantSummary
        {
            public Guid Id { get; set; }
            public Guid OwnerUserId { get; set; }
            public string Name { get; set; } = string.Empty;
            public MerchantType Type { get; set; }
            public MerchantStatus Status { get; set; }
            public MerchantKycStatus KycStatus { get; set; }
            public DateTime CreatedAtUtc { get; set; }
        }
    }
}
