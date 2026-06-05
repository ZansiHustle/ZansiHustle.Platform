using System;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// Rolling lifetime engagement counters + a derived trending score for a
    /// single normal <c>Listing</c>. Maintained incrementally as events are
    /// tracked. One row per <see cref="ListingId"/> (unique index). All
    /// timestamps are UTC.
    /// </summary>
    public class ZansiPulseListingMetric
    {
        public Guid Id { get; set; }

        public Guid ListingId { get; set; }

        public int TotalViews { get; set; }
        public int TotalDetailOpens { get; set; }
        public int TotalFavourites { get; set; }
        public int TotalShares { get; set; }
        public int TotalMessages { get; set; }
        public int TotalReports { get; set; }

        /// <summary>
        /// Recency-decayed weighted engagement score. Recomputed on each
        /// tracked event from the counters + last-engagement recency. Used to
        /// rank trending listings and as the engagement factor in
        /// recommendations.
        /// </summary>
        public decimal TrendingScore { get; set; }

        public DateTime? LastEngagementAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
