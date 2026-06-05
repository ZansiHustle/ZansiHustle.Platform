using System;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// Aggregate performance + quality signals for a seller (a
    /// <c>Merchant</c>). Engagement counters are maintained incrementally;
    /// the derived scores (responsiveness / trust / popularity / quality) are
    /// recomputed during snapshot runs and on relevant events. One row per
    /// <see cref="SellerId"/> (unique index). All timestamps are UTC.
    /// </summary>
    public class ZansiPulseSellerMetric
    {
        public Guid Id { get; set; }

        public Guid SellerId { get; set; }

        public int TotalListings { get; set; }
        public int TotalViews { get; set; }
        public int TotalFavourites { get; set; }
        public int TotalMessages { get; set; }
        public int TotalReports { get; set; }

        /// <summary>0–100. Placeholder until message-response timing is wired; defaults neutral.</summary>
        public decimal ResponsivenessScore { get; set; }

        /// <summary>0–100. Penalised by reports, lifted by reviews/orders.</summary>
        public decimal TrustScore { get; set; }

        /// <summary>0–100. Driven by views / favourites / message volume.</summary>
        public decimal PopularityScore { get; set; }

        /// <summary>0–100. Blended quality signal (rating + trust + popularity).</summary>
        public decimal QualityScore { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
