using System;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// An audit record of a recommendation set ZansiPulse served — what was
    /// returned, to whom, by which strategy. Lets us later measure
    /// recommendation quality (impression → engagement) and reproduce a
    /// served set. The id collections are stored as JSON arrays rather than
    /// join rows because this is a write-once log, not a queried relation.
    /// All timestamps are UTC.
    /// </summary>
    public class ZansiPulseRecommendationLog
    {
        public Guid Id { get; set; }

        public Guid? UserId { get; set; }

        /// <summary>e.g. "Listings", "Shops".</summary>
        public string RecommendationType { get; set; } = string.Empty;

        /// <summary>Strategy that produced the set, e.g. "Personalized", "Fallback".</summary>
        public string Source { get; set; } = string.Empty;

        public string? ListingIdsJson { get; set; }
        public string? ShopIdsJson { get; set; }
        public string? SellerIdsJson { get; set; }
        public string? CategoryIdsJson { get; set; }
        public string? RequestMetadataJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
