using System;
using ZansiHustle.Shared.Enums.ZansiPulse;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// Per-period metrics for a raw search term, optionally scoped by region
    /// and category. One row per (<see cref="SearchTerm"/>,
    /// <see cref="Province"/>, <see cref="City"/>, <see cref="CategoryId"/>,
    /// <see cref="PeriodType"/>, <see cref="PeriodStart"/>). Powers search
    /// insights and (with low/zero result counts) the demand side of
    /// supply/demand gaps. All timestamps are UTC.
    /// </summary>
    public class ZansiPulseSearchTermMetric
    {
        public Guid Id { get; set; }

        public string SearchTerm { get; set; } = string.Empty;

        public string? Province { get; set; }
        public string? City { get; set; }
        public Guid? CategoryId { get; set; }

        public int SearchCount { get; set; }

        /// <summary>Total results shown across searches in this bucket (when the search API reports it).</summary>
        public int ResultCount { get; set; }

        /// <summary>Searches that led to a result tap.</summary>
        public int ClickThroughCount { get; set; }

        /// <summary>Searches that returned nothing — strong unmet-demand signal.</summary>
        public int NoResultCount { get; set; }

        public ZansiPulsePeriodType PeriodType { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
