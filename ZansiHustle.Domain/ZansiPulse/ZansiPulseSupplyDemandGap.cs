using System;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// A computed supply-vs-demand gap for a slice of the market — a
    /// category, region, and/or search term where demand (searches, views,
    /// messages) outstrips available supply (active listings). Rebuilt during
    /// snapshot runs. A high <see cref="GapScore"/> flags an opportunity for
    /// sellers / sourcing. All timestamps are UTC.
    /// </summary>
    public class ZansiPulseSupplyDemandGap
    {
        public Guid Id { get; set; }

        public Guid? CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SearchTerm { get; set; }

        /// <summary>Aggregate demand signal for this slice (searches + views + messages, weighted).</summary>
        public decimal DemandScore { get; set; }

        /// <summary>Count of active listings serving this slice.</summary>
        public int SupplyCount { get; set; }

        /// <summary>Demand relative to supply — higher means a bigger unmet gap.</summary>
        public decimal GapScore { get; set; }

        /// <summary>Optional human-readable suggestion (e.g. "Recruit sellers in this category/region").</summary>
        public string? RecommendedAction { get; set; }

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
