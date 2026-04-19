using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Persistence.Referrals;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Application.Referrals.Dtos;
using ZansiHustle.Domain.Referrals;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Referrals
{
    /// <summary>
    /// Standalone referral / affiliate service. See IReferralService for
    /// the contract. Implementation notes:
    ///
    /// - Slug generation uses {firstName}{lastInitial} lowercased, stripped
    ///   to URL-safe chars. Conflicts resolve by appending a numeric suffix
    ///   (proficientm, proficientm2, …). Slug is stable once issued so old
    ///   share-links keep working.
    ///
    /// - Counter increments are best-effort: a click/record write that
    ///   fails to bump the counter does not roll back the click row. The
    ///   counters are denormalised, the source-of-truth rows still exist.
    ///
    /// - RecordReferralAsync is idempotent: the (ReferredUserId, Type)
    ///   unique index plus a check-then-insert avoids duplicate rows when
    ///   onboarding fires the call more than once.
    /// </summary>
    public class ReferralService : IReferralService
    {
        private readonly IAffiliateProfileRepository _profiles;
        private readonly IUserReferralRepository _referrals;
        private readonly IReferralClickRepository _clicks;
        private readonly IUserRepository _users;
        private readonly ICurrentUserService _currentUser;
        private readonly IConfiguration _config;

        public ReferralService(
            IAffiliateProfileRepository profiles,
            IUserReferralRepository referrals,
            IReferralClickRepository clicks,
            IUserRepository users,
            ICurrentUserService currentUser,
            IConfiguration config)
        {
            _profiles = profiles;
            _referrals = referrals;
            _clicks = clicks;
            _users = users;
            _currentUser = currentUser;
            _config = config;
        }

        public async Task<Result<AffiliateProfileDto>> GetOrCreateMineAsync()
        {
            if (!_currentUser.UserId.HasValue)
                return Result<AffiliateProfileDto>.Failure(ErrorCodes.Unauthorized, "Authenticated user was not found.");

            var userId = _currentUser.UserId.Value;
            var existing = await _profiles.GetByUserIdAsync(userId);
            if (existing is not null)
                return Result<AffiliateProfileDto>.Success(MapToDto(existing), "Affiliate profile retrieved.");

            var user = await _users.GetByIdAsync(userId);
            if (user is null)
                return Result<AffiliateProfileDto>.Failure(ErrorCodes.NotFound, "User not found.");

            var slug = await GenerateUniqueSlugAsync(user.FirstName, user.LastName, _currentUser.Email);
            var profile = new AffiliateProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ReferralCode = slug,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _profiles.AddAsync(profile);
            var saved = await _profiles.SaveChangesAsync();
            if (!saved)
                return Result<AffiliateProfileDto>.Failure(ErrorCodes.Exception, "Failed to create affiliate profile.");

            // Reload with User include for display name.
            var reloaded = await _profiles.GetByIdAsync(profile.Id) ?? profile;
            return Result<AffiliateProfileDto>.Success(MapToDto(reloaded), "Affiliate profile created.");
        }

        public async Task<Result<AffiliateProfileDto>> GetByIdAsync(Guid id)
        {
            var profile = await _profiles.GetByIdAsync(id);
            if (profile is null)
                return Result<AffiliateProfileDto>.Failure(ErrorCodes.NotFound, "Affiliate profile not found.");
            return Result<AffiliateProfileDto>.Success(MapToDto(profile), "OK");
        }

        public async Task<Result<ResolveReferralDto>> ResolveBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return Result<ResolveReferralDto>.Failure(ErrorCodes.NotFound, "Referral code not found.");

            var profile = await _profiles.GetByCodeAsync(slug);
            if (profile is null || !profile.IsActive)
                return Result<ResolveReferralDto>.Failure(ErrorCodes.NotFound, "Referral code not found.");

            var displayName = profile.User is null
                ? null
                : $"{profile.User.FirstName} {profile.User.LastName}".Trim();

            return Result<ResolveReferralDto>.Success(new ResolveReferralDto
            {
                AffiliateProfileId = profile.Id,
                ReferrerUserId = profile.UserId,
                ReferralCode = profile.ReferralCode,
                ReferrerDisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName,
                IsActive = profile.IsActive,
            }, "OK");
        }

        public async Task<Result> RecordClickAsync(RecordReferralClickRequestDto request, string? ipHash)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.ReferralCode))
                return Result.Failure(ErrorCodes.BadRequest, "Referral code is required.");

            var profile = await _profiles.GetByCodeAsync(request.ReferralCode);
            if (profile is null || !profile.IsActive)
                // Silent success on unknown codes — public endpoint, don't
                // leak which slugs exist via 404 vs 200.
                return Result.Success("Click recorded.");

            var click = new ReferralClick
            {
                Id = Guid.NewGuid(),
                AffiliateProfileId = profile.Id,
                ReferralCode = profile.ReferralCode,
                LandingPath = Truncate(request.LandingPath, 500),
                UserAgent = Truncate(request.UserAgent, 500),
                IpHash = ipHash,
                ClickedAtUtc = DateTime.UtcNow,
            };

            await _clicks.AddAsync(click);
            await _clicks.SaveChangesAsync();

            // Bump the denormalised counter on the profile. Separate save
            // so a counter-write failure doesn't lose the click row.
            profile.ClickCount += 1;
            profile.UpdatedAtUtc = DateTime.UtcNow;
            _profiles.Update(profile);
            await _profiles.SaveChangesAsync();

            return Result.Success("Click recorded.");
        }

        public async Task<Result<Guid>> RecordReferralAsync(Guid referredUserId, RecordReferralRequestDto request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.ReferralCode))
                return Result<Guid>.Failure(ErrorCodes.BadRequest, "Referral code is required.");

            // Idempotent: if the user already has a referral row of this
            // type, return its id and do nothing else. Prevents duplicate
            // attribution if the client retries.
            var existing = await _referrals.GetByReferredUserAsync(referredUserId, request.ReferralType);
            if (existing is not null)
                return Result<Guid>.Success(existing.Id, "Already attributed.");

            var profile = await _profiles.GetByCodeAsync(request.ReferralCode);
            if (profile is null || !profile.IsActive)
                return Result<Guid>.Failure(ErrorCodes.NotFound, "Referral code not found.");

            // Self-referral guard. If we ever support it, add an explicit
            // setting; for now silently ignore.
            if (profile.UserId == referredUserId)
                return Result<Guid>.Failure(ErrorCodes.BadRequest, "A user cannot refer themselves.");

            var entity = new UserReferral
            {
                Id = Guid.NewGuid(),
                AffiliateProfileId = profile.Id,
                ReferrerUserId = profile.UserId,
                ReferredUserId = referredUserId,
                ReferralCodeUsed = profile.ReferralCode,
                ReferralType = request.ReferralType,
                Status = Shared.Enums.Referrals.ReferralStatus.Joined,
                SourcePath = Truncate(request.SourcePath, 500),
                JoinedAtUtc = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _referrals.AddAsync(entity);
            await _referrals.SaveChangesAsync();

            // Bump JoinCount. Conversion is incremented later by whichever
            // pipeline considers the user "converted" (e.g. seller approved).
            profile.JoinCount += 1;
            profile.UpdatedAtUtc = DateTime.UtcNow;
            _profiles.Update(profile);
            await _profiles.SaveChangesAsync();

            return Result<Guid>.Success(entity.Id, "Referral recorded.");
        }

        // ── Slug generation ───────────────────────────────────────────────
        private async Task<string> GenerateUniqueSlugAsync(string firstName, string lastName, string? email)
        {
            var baseSlug = BuildBaseSlug(firstName, lastName, email);
            var candidate = baseSlug;
            var i = 2;
            while (await _profiles.CodeExistsAsync(candidate))
            {
                candidate = $"{baseSlug}{i++}";
                if (i > 1000)
                {
                    // Practically impossible; fall back to a random suffix
                    // so the loop can never run away.
                    candidate = $"{baseSlug}{Guid.NewGuid().ToString("n")[..6]}";
                    break;
                }
            }
            return candidate;
        }

        private static string BuildBaseSlug(string firstName, string lastName, string? email)
        {
            var first = Slugify(firstName);
            var lastInitial = string.IsNullOrWhiteSpace(lastName)
                ? string.Empty
                : Slugify(lastName.Substring(0, 1));

            var slug = $"{first}{lastInitial}";

            if (string.IsNullOrWhiteSpace(slug) && !string.IsNullOrWhiteSpace(email))
                slug = Slugify(email.Split('@')[0]);

            if (string.IsNullOrWhiteSpace(slug))
                slug = $"affiliate{DateTime.UtcNow:yyyyMMddHHmmss}";

            // Cap to a sane length so suffixes always fit under the 60-char
            // column limit.
            return slug.Length > 40 ? slug.Substring(0, 40) : slug;
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var lower = value.Trim().ToLowerInvariant();
            // Strip accents.
            var stripped = new string(lower.Normalize(NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                    != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());
            return Regex.Replace(stripped, "[^a-z0-9]", "");
        }

        // ── Mapping ───────────────────────────────────────────────────────
        private AffiliateProfileDto MapToDto(AffiliateProfile p)
        {
            return new AffiliateProfileDto
            {
                Id = p.Id,
                UserId = p.UserId,
                UserDisplayName = p.User is null
                    ? null
                    : $"{p.User.FirstName} {p.User.LastName}".Trim(),
                ReferralCode = p.ReferralCode,
                ReferralLink = BuildReferralLink(p.ReferralCode),
                IsActive = p.IsActive,
                ClickCount = p.ClickCount,
                JoinCount = p.JoinCount,
                ConversionCount = p.ConversionCount,
                CommissionRate = p.CommissionRate,
                Tier = p.Tier,
                Notes = p.Notes,
                CreatedAtUtc = p.CreatedAtUtc,
                UpdatedAtUtc = p.UpdatedAtUtc,
            };
        }

        private string BuildReferralLink(string slug)
        {
            // Configurable host so dev and prod render the right URL. Falls
            // back to the production portal host when unset.
            var host = _config["Referrals:PortalBaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(host))
                host = "https://portal.zansihustle.co.za";
            return $"{host}/join/{slug}";
        }

        private static string? Truncate(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= max ? value : value.Substring(0, max);
        }

        /// <summary>
        /// SHA-256 hex of an IP. Salt could come from config; for now we use
        /// a fixed prefix so the same IP from the same client always hashes
        /// the same way (allows light per-IP grouping without storing raw).
        /// </summary>
        public static string? HashIp(string? rawIp)
        {
            if (string.IsNullOrWhiteSpace(rawIp)) return null;
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes("zh-referral|" + rawIp.Trim());
            return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
        }
    }
}
