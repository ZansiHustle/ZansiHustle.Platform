using System;
using ZansiHustle.Shared.Enums.ZansiPulse;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// A point-in-time rollup of the whole platform's pulse for a period —
    /// the precomputed payload the CEO/admin dashboard reads so it never has
    /// to aggregate the raw event stream on request. Top-N collections are
    /// stored as JSON blobs. All timestamps are UTC.
    /// </summary>
    public class ZansiPulseSnapshot
    {
        public Guid Id { get; set; }

        public ZansiPulsePeriodType SnapshotType { get; set; }

        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public int TotalEvents { get; set; }

        public string? TopCategoriesJson { get; set; }
        public string? TopRegionsJson { get; set; }
        public string? TopListingsJson { get; set; }
        public string? TopSellersJson { get; set; }
        public string? TopSearchTermsJson { get; set; }
        public string? SupplyDemandGapsJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
