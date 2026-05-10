using System;
using ZansiHustle.Shared.Enums.Reviews;

namespace ZansiHustle.Domain.Reviews
{
    /// <summary>
    /// One reviewer rating + comment about a single target (Store /
    /// Shop / Product / Service — see <see cref="ReviewTargetType"/>).
    ///
    /// Polymorphic via (TargetType, TargetId) rather than concrete FK
    /// columns so the same table backs every reviewable surface in
    /// the app. The only enforced relationship is to the reviewer
    /// (User); the target identifier is validated in the service
    /// layer for each TargetType.
    /// </summary>
    public class Review
    {
        public Guid Id { get; set; }

        /// <summary>Discriminator for which kind of entity this row is reviewing.</summary>
        public ReviewTargetType TargetType { get; set; }

        /// <summary>Id of the merchant / listing / etc. being reviewed.</summary>
        public Guid TargetId { get; set; }

        /// <summary>FK to AspNetUsers (User.Id is Guid).</summary>
        public Guid ReviewerUserId { get; set; }

        /// <summary>1..5, validated in the service layer + at the database (CHECK).</summary>
        public int Rating { get; set; }

        /// <summary>Optional free-text comment. Length cap enforced by the column config.</summary>
        public string? Comment { get; set; }

        public ReviewStatus Status { get; set; } = ReviewStatus.Active;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        /// <summary>Set when <see cref="Status"/> flips to <see cref="ReviewStatus.Deleted"/>.</summary>
        public DateTime? DeletedAtUtc { get; set; }
    }
}
