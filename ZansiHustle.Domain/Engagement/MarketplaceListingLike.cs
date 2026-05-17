using System;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Marketplace;

namespace ZansiHustle.Domain.Engagement
{
    /// <summary>
    /// "Heart" / like by a buyer on a casual <see cref="MarketplaceListing"/>.
    ///
    /// Deliberately a separate table from <see cref="ListingLike"/> —
    /// MarketplaceListings are a different domain (User-owned, no
    /// Merchant, no orders) and we want a clean FK to the right parent.
    /// Mixing into one polymorphic table would force loose FK
    /// validation in code instead of at the database level.
    ///
    /// One row per (UserId, MarketplaceListingId). Liked count is
    /// denormalised onto <see cref="MarketplaceListing.LikeCount"/> and
    /// updated transactionally alongside this row.
    /// </summary>
    public class MarketplaceListingLike
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid MarketplaceListingId { get; set; }
        public MarketplaceListing? MarketplaceListing { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
