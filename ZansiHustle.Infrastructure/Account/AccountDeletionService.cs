using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Account;
using ZansiHustle.Application.Persistence.Identity;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.Reviews;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Reviews;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Account
{
    /// <summary>
    /// Anonymising account deletion. Lives in Infrastructure (queries
    /// <see cref="AppDbContext"/> directly, same convention as ChatService /
    /// AgentPayoutService) because it spans many aggregates. See
    /// <see cref="IAccountDeletionService"/> for the compliance rationale.
    /// </summary>
    public sealed class AccountDeletionService : IAccountDeletionService
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenGenerator _jwt;
        private readonly ILogger<AccountDeletionService> _logger;

        public AccountDeletionService(
            AppDbContext context,
            IJwtTokenGenerator jwt,
            ILogger<AccountDeletionService> logger)
        {
            _context = context;
            _jwt = jwt;
            _logger = logger;
        }

        public async Task<Result> DeleteMyAccountAsync(Guid userId, string? reason)
        {
            try
            {
                var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == userId);
                if (user is null)
                    return Result.Failure(ErrorCodes.NotFound, "Account not found.");

                // Idempotent: an already-deleted account is a success (the client
                // may retry after a dropped connection).
                if (user.AccountStatus == AccountStatus.Deleted)
                    return Result.Success("Account already deleted.");

                var now = DateTime.UtcNow;
                var anonEmail = $"deleted-{user.Id:N}@deleted.zansihustle.invalid";

                // 1. Identity / login PII — makes the account permanently
                //    inaccessible (auth layer rejects any non-Active status) and
                //    strips the email/phone/name.
                user.Email = anonEmail;
                user.NormalizedEmail = anonEmail.ToUpperInvariant();
                user.UserName = anonEmail;
                user.NormalizedUserName = anonEmail.ToUpperInvariant();
                user.PhoneNumber = null;
                user.PhoneNumberConfirmed = false;
                user.EmailConfirmed = false;
                user.FirstName = "Deleted";
                user.LastName = "User";
                user.IsActive = false;
                user.AccountStatus = AccountStatus.Deleted;
                user.UpdatedOnUtc = now;
                // New security stamp + unusable password hash → any cached
                // credential / token is invalidated and the row can't be signed in.
                user.SecurityStamp = Guid.NewGuid().ToString();
                user.PasswordHash = null;

                // 2. Extended profile PII.
                var profile = await _context.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile is not null)
                {
                    profile.ProfileImageUrl = null;
                    profile.Bio = null;
                    profile.City = null;
                    profile.Province = null;
                    profile.UpdatedOnUtc = now;
                }

                // 3. Notification preferences off (defence-in-depth; devices are
                //    unusable once tokens are revoked anyway).
                var settings = await _context.Set<UserSettings>().FirstOrDefaultAsync(s => s.UserId == userId);
                if (settings is not null)
                {
                    settings.EmailNotificationsEnabled = false;
                    settings.PushNotificationsEnabled = false;
                }

                // 4. Seller/merchant accounts owned by the user — strip business
                //    PII (name, contact, ID number, bank, address, branding) and
                //    Suspend so the merchant + its shops/listings drop out of all
                //    public discovery surfaces.
                var merchants = await _context.Set<Merchant>()
                    .Where(m => m.OwnerUserId == userId)
                    .ToListAsync();
                foreach (var m in merchants)
                {
                    m.Name = "Deleted seller";
                    m.Description = null;
                    m.ContactEmail = null;
                    m.ContactPhoneNumber = null;
                    m.WhatsAppNumber = null;
                    m.SocialHandle = null;
                    m.IdNumber = null;
                    m.BankName = null;
                    m.BankAccountHolder = null;
                    m.BankAccountNumber = null;
                    m.BankAccountType = null;
                    m.BankBranchCode = null;
                    m.IsBankVerified = false;
                    m.AddressLine1 = null;
                    m.Suburb = null;
                    m.PostalCode = null;
                    m.FormattedAddress = null;
                    m.Latitude = null;
                    m.Longitude = null;
                    m.GooglePlaceId = null;
                    m.ProfileImageUrl = null;
                    m.LogoUrl = null;
                    m.BannerUrl = null;
                    m.WebsiteUrl = null;
                    m.Status = MerchantStatus.Suspended;
                    m.UpdatedAtUtc = now;
                }

                // 5. Buyer PII snapshotted onto the user's own orders (the money
                //    rows are retained for accounting, but the personal snapshot
                //    is anonymised).
                var orders = await _context.Set<Order>()
                    .Where(o => o.BuyerUserId == userId)
                    .ToListAsync();
                foreach (var o in orders)
                {
                    o.BuyerName = "Deleted user";
                    o.BuyerEmail = null;
                    o.BuyerPhone = null;
                    o.UpdatedAtUtc = now;
                }

                // 6. Remove the user's public reviews (UGC) from discovery.
                var reviews = await _context.Set<Review>()
                    .Where(r => r.ReviewerUserId == userId && r.Status != ReviewStatus.Deleted)
                    .ToListAsync();
                foreach (var r in reviews)
                {
                    r.Status = ReviewStatus.Deleted;
                    r.DeletedAtUtc = now;
                    r.UpdatedAtUtc = now;
                }

                await _context.SaveChangesAsync();

                // 7. Revoke every refresh token → all sessions on all devices die.
                await _jwt.RevokeAllRefreshTokensForUserAsync(userId);

                _logger.LogInformation(
                    "Account {UserId} deleted (anonymised). merchants={Merchants} orders={Orders} reviews={Reviews} reason={Reason}",
                    userId, merchants.Count, orders.Count, reviews.Count,
                    string.IsNullOrWhiteSpace(reason) ? "(none)" : reason);

                return Result.Success("Your account has been permanently deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account deletion failed for user {UserId}.", userId);
                return Result.Failure(ErrorCodes.Exception,
                    "We couldn't delete your account right now. Please try again.");
            }
        }
    }
}
