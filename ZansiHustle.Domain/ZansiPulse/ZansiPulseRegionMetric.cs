using System;
using ZansiHustle.Shared.Enums.ZansiPulse;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// Per-period demand signals for a region (province, optional city). One
    /// row per (<see cref="Province"/>, <see cref="City"/>,
    /// <see cref="PeriodType"/>, <see cref="PeriodStart"/>); a unique index
    /// enforces the bucket. Powers trending regions and regional insights.
    /// All timestamps are UTC.
    /// </summary>
    public class ZansiPulseRegionMetric
    {
        public Guid Id { get; set; }

        public string Province { get; set; } = string.Empty;
        public string? City { get; set; }

        public ZansiPulsePeriodType PeriodType { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public int TotalViews { get; set; }
        public int TotalSearches { get; set; }
        public int TotalFavourites { get; set; }
        public int TotalMessages { get; set; }

        public decimal TrendingScore { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
