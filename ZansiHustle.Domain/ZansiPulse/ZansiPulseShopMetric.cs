using System;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// Aggregate engagement + quality signals for an online <c>ShopProfile</c>
    /// storefront. One row per <see cref="ShopId"/> (unique index). All
    /// timestamps are UTC.
    /// </summary>
    public class ZansiPulseShopMetric
    {
        public Guid Id { get; set; }

        public Guid ShopId { get; set; }

        public int TotalViews { get; set; }
        public int TotalFavourites { get; set; }
        public int TotalShares { get; set; }
        public int TotalMessages { get; set; }
        public int TotalReports { get; set; }

        /// <summary>0–100. Driven by views / follows / message volume.</summary>
        public decimal PopularityScore { get; set; }

        /// <summary>0–100. Blended quality signal (rating + reports).</summary>
        public decimal QualityScore { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
