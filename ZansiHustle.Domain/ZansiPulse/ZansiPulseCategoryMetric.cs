using System;
using ZansiHustle.Shared.Enums.ZansiPulse;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// Per-period demand signals for a (category, optional subcategory). One
    /// row per (<see cref="CategoryId"/>, <see cref="SubCategoryId"/>,
    /// <see cref="PeriodType"/>, <see cref="PeriodStart"/>); a unique index
    /// enforces the bucket. Powers trending categories and the demand side of
    /// supply/demand analysis. All timestamps are UTC.
    /// </summary>
    public class ZansiPulseCategoryMetric
    {
        public Guid Id { get; set; }

        public Guid CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }

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
