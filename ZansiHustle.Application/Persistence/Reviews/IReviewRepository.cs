using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Reviews;
using ZansiHustle.Shared.Enums.Reviews;

namespace ZansiHustle.Application.Persistence.Reviews
{
    /// <summary>
    /// Display-name + avatar tuple returned by
    /// <see cref="IReviewRepository.GetReviewerDisplaysAsync"/>. Kept
    /// in the persistence layer so the application service stays
    /// agnostic of the underlying join (User table + UserProfiles).
    /// </summary>
    public record ReviewerDisplayInfo(string DisplayName, string? AvatarUrl);

    /// <summary>
    /// Persistence contract for <see cref="Review"/>. All listing /
    /// summary queries scope to <see cref="ReviewStatus.Active"/> by
    /// default; <see cref="GetByIdAsync"/> returns the row regardless
    /// so the service layer can act on soft-deleted rows for owner
    /// edits / undelete in the future.
    /// </summary>
    public interface IReviewRepository
    {
        /// <summary>Loads a single review by id, regardless of status.</summary>
        Task<Review?> GetByIdAsync(Guid id);

        /// <summary>
        /// Lists Active reviews for a target, newest first. Used for
        /// the public reviews list. The service layer builds the
        /// reviewer display via <see cref="GetReviewerDisplaysAsync"/>.
        /// </summary>
        Task<List<Review>> GetActiveForTargetAsync(ReviewTargetType targetType, Guid targetId);

        /// <summary>
        /// Returns the current user's Active review for a target, if
        /// any. Used to gate the "Write a review" / "Edit your review"
        /// affordance and to enforce one-active-per-user.
        /// </summary>
        Task<Review?> GetActiveByReviewerAsync(ReviewTargetType targetType, Guid targetId, Guid reviewerUserId);

        /// <summary>
        /// (avg, count) over Active reviews. avg is null when count
        /// is 0 — keeps the wire contract aligned with `Merchant.Rating`.
        /// </summary>
        Task<(decimal? Average, int Count)> GetSummaryAsync(ReviewTargetType targetType, Guid targetId);

        /// <summary>
        /// Batched display-name + avatar lookup for a set of reviewer
        /// user ids. Returns a dictionary keyed by user id so the
        /// service can flatten into ReviewDtos without an N+1 query.
        /// Lives in the repository because it joins User + UserProfile
        /// across DbSets that the application layer cannot reach
        /// directly (no AppDbContext reference outside Infrastructure).
        /// </summary>
        Task<Dictionary<Guid, ReviewerDisplayInfo>> GetReviewerDisplaysAsync(IEnumerable<Guid> userIds);

        Task AddAsync(Review review);
        void Update(Review review);
        Task<bool> SaveChangesAsync();
    }
}
