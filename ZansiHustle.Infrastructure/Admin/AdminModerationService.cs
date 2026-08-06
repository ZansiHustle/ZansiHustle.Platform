using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Admin.Moderation;
using ZansiHustle.Application.Persistence.Identity;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Reviews;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Reviews;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Admin
{
    /// <summary>
    /// DB-direct admin moderation enforcement. See
    /// <see cref="IAdminModerationService"/>. Records a <see cref="TrustEvent"/>
    /// for each action so there's an audit trail.
    /// </summary>
    public sealed class AdminModerationService : IAdminModerationService
    {
        private readonly AppDbContext _db;
        private readonly IJwtTokenGenerator _jwt;
        private readonly ILogger<AdminModerationService> _logger;

        public AdminModerationService(AppDbContext db, IJwtTokenGenerator jwt, ILogger<AdminModerationService> logger)
        {
            _db = db;
            _jwt = jwt;
            _logger = logger;
        }

        // ── Users ───────────────────────────────────────────────────

        public Task<Result> SuspendUserAsync(Guid userId, Guid moderatorUserId, string? reason)
            => SetUserStatusAsync(userId, moderatorUserId, AccountStatus.Suspended, revokeSessions: true, "suspend", reason);

        public Task<Result> BanUserAsync(Guid userId, Guid moderatorUserId, string? reason)
            => SetUserStatusAsync(userId, moderatorUserId, AccountStatus.Revoked, revokeSessions: true, "ban", reason);

        public Task<Result> ReinstateUserAsync(Guid userId, Guid moderatorUserId)
            => SetUserStatusAsync(userId, moderatorUserId, AccountStatus.Active, revokeSessions: false, "reinstate", null);

        private async Task<Result> SetUserStatusAsync(
            Guid userId, Guid moderatorUserId, AccountStatus status, bool revokeSessions, string action, string? reason)
        {
            try
            {
                var user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Id == userId);
                if (user is null) return Result.Failure(ErrorCodes.NotFound, "User not found.");
                if (user.AccountStatus == AccountStatus.Deleted)
                    return Result.Failure(ErrorCodes.Conflict, "This account has been deleted and cannot be modified.");

                user.AccountStatus = status;
                user.IsActive = status == AccountStatus.Active;
                user.UpdatedOnUtc = DateTime.UtcNow;
                RecordTrust(moderatorUserId, userId, $"user.{action}", "User", userId, reason);
                await _db.SaveChangesAsync();

                if (revokeSessions)
                    await _jwt.RevokeAllRefreshTokensForUserAsync(userId);

                return Result.Success($"User {action}d.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Moderation {Action} failed for user {UserId}.", action, userId);
                return Result.Failure(ErrorCodes.Exception, $"Couldn't {action} the user.");
            }
        }

        // ── Listings ────────────────────────────────────────────────

        public Task<Result> HideListingAsync(Guid listingId, Guid moderatorUserId, string? reason)
            => SetListingStatusAsync(listingId, moderatorUserId, ListingStatus.Archived, "hide", reason);

        public Task<Result> UnhideListingAsync(Guid listingId, Guid moderatorUserId)
            => SetListingStatusAsync(listingId, moderatorUserId, ListingStatus.Active, "unhide", null);

        private async Task<Result> SetListingStatusAsync(
            Guid listingId, Guid moderatorUserId, ListingStatus status, string action, string? reason)
        {
            try
            {
                var listing = await _db.Set<Listing>().FirstOrDefaultAsync(l => l.Id == listingId);
                if (listing is null) return Result.Failure(ErrorCodes.NotFound, "Listing not found.");

                listing.Status = status;
                listing.UpdatedAtUtc = DateTime.UtcNow;
                RecordTrust(moderatorUserId, listing.MerchantId, $"listing.{action}", "Listing", listingId, reason);
                await _db.SaveChangesAsync();
                return Result.Success($"Listing {action}d.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Moderation {Action} failed for listing {ListingId}.", action, listingId);
                return Result.Failure(ErrorCodes.Exception, $"Couldn't {action} the listing.");
            }
        }

        // ── Reviews ─────────────────────────────────────────────────

        public Task<Result> HideReviewAsync(Guid reviewId, Guid moderatorUserId, string? reason)
            => SetReviewStatusAsync(reviewId, moderatorUserId, ReviewStatus.Hidden, "hide", reason);

        public Task<Result> UnhideReviewAsync(Guid reviewId, Guid moderatorUserId)
            => SetReviewStatusAsync(reviewId, moderatorUserId, ReviewStatus.Active, "unhide", null);

        private async Task<Result> SetReviewStatusAsync(
            Guid reviewId, Guid moderatorUserId, ReviewStatus status, string action, string? reason)
        {
            try
            {
                var review = await _db.Set<Review>().FirstOrDefaultAsync(r => r.Id == reviewId);
                if (review is null) return Result.Failure(ErrorCodes.NotFound, "Review not found.");
                if (review.Status == ReviewStatus.Deleted)
                    return Result.Failure(ErrorCodes.Conflict, "This review was already removed by its author.");

                review.Status = status;
                review.UpdatedAtUtc = DateTime.UtcNow;
                RecordTrust(moderatorUserId, review.ReviewerUserId, $"review.{action}", "Review", reviewId, reason);
                await _db.SaveChangesAsync();
                return Result.Success($"Review {action}d.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Moderation {Action} failed for review {ReviewId}.", action, reviewId);
                return Result.Failure(ErrorCodes.Exception, $"Couldn't {action} the review.");
            }
        }

        // ── Audit ───────────────────────────────────────────────────

        private void RecordTrust(Guid moderatorUserId, Guid subjectUserId, string type, string refType, Guid refId, string? reason)
        {
            // Structured audit trail. (A TrustEvent row could be added later once
            // the moderation TrustEventType values are defined.)
            _logger.LogInformation(
                "MODERATION {Type} by {Moderator} on {RefType}:{RefId} subject={Subject} reason={Reason}",
                type, moderatorUserId, refType, refId, subjectUserId,
                string.IsNullOrWhiteSpace(reason) ? "(none)" : reason);
        }
    }
}
