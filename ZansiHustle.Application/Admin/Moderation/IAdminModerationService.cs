using System;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Moderation
{
    /// <summary>Body carrying an optional moderator reason.</summary>
    public class ModerationActionDto
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Admin moderation enforcement (App Store Guideline 1.2 — the platform must
    /// be able to remove content and suspend/ban offending users). All actions
    /// are reversible except the underlying financial records.
    /// </summary>
    public interface IAdminModerationService
    {
        // Users — status flips are enforced by the auth layer (non-Active
        // accounts cannot log in / refresh).
        Task<Result> SuspendUserAsync(Guid userId, Guid moderatorUserId, string? reason);
        Task<Result> BanUserAsync(Guid userId, Guid moderatorUserId, string? reason);
        Task<Result> ReinstateUserAsync(Guid userId, Guid moderatorUserId);

        // Content takedown / restore.
        Task<Result> HideListingAsync(Guid listingId, Guid moderatorUserId, string? reason);
        Task<Result> UnhideListingAsync(Guid listingId, Guid moderatorUserId);
        Task<Result> HideReviewAsync(Guid reviewId, Guid moderatorUserId, string? reason);
        Task<Result> UnhideReviewAsync(Guid reviewId, Guid moderatorUserId);
    }
}
