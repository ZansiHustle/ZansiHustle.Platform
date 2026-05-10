using System;
using ZansiHustle.Shared.Enums.Reviews;

namespace ZansiHustle.Application.Reviews.Dtos
{
    /// <summary>
    /// Public projection of a single Review. The reviewer's display
    /// name comes from User.FirstName + LastName (joined server-side
    /// to keep the wire DTO stable even when the underlying user
    /// model evolves). `IsMine` lets the client render edit/delete
    /// affordances without a second roundtrip.
    /// </summary>
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public ReviewTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        public Guid ReviewerUserId { get; set; }
        public string ReviewerDisplayName { get; set; } = string.Empty;
        /// <summary>Optional avatar URL. Null when the user hasn't set a profile photo.</summary>
        public string? ReviewerAvatarUrl { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        /// <summary>True iff the current authenticated user owns this review.</summary>
        public bool IsMine { get; set; }
    }
}
