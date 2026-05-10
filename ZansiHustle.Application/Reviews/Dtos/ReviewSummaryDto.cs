using System;
using ZansiHustle.Shared.Enums.Reviews;

namespace ZansiHustle.Application.Reviews.Dtos
{
    /// <summary>
    /// Aggregate for a target — the numbers the StoreProfile / shop
    /// header / etc. need to render the rating row, plus an
    /// embedded `MyReview` so authenticated callers can show the
    /// "Edit your review" CTA without a second list roundtrip.
    /// </summary>
    public class ReviewSummaryDto
    {
        public ReviewTargetType TargetType { get; set; }
        public Guid TargetId { get; set; }
        /// <summary>
        /// Average of all Active reviews' ratings. <c>null</c> when
        /// the target has zero reviews — matches the existing
        /// `Merchant.Rating` contract where a non-null rating
        /// implies `ReviewCount > 0`.
        /// </summary>
        public decimal? AverageRating { get; set; }
        public int ReviewCount { get; set; }
        /// <summary>Populated only when the caller is authenticated and has reviewed this target.</summary>
        public ReviewDto? MyReview { get; set; }
    }
}
