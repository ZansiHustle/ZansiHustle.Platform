using System;
using System.Collections.Generic;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.Marketplace;

namespace ZansiHustle.Domain.Marketplace
{
    /// <summary>
    /// Casual peer-to-peer Marketplace listing — second-hand items posted
    /// by ordinary users (not approved merchants).
    ///
    /// Intentionally separate from <see cref="Listings.Listing"/>:
    ///   • Owner is a <see cref="User"/>, NOT a Merchant.
    ///   • No SellerCategory / shop / fulfillment integration.
    ///   • No payment / order machinery — buyers contact sellers directly.
    /// </summary>
    public class MarketplaceListing
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Casual seller — references <see cref="User.Id"/>. Identifies
        /// who can edit/mark-sold and who buyers should be routed to in
        /// chat. There is NO MerchantId on this entity by design.
        /// </summary>
        public Guid OwnerUserId { get; set; }
        public User? Owner { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZAR";

        /// <summary>
        /// Free-text category chosen by the seller (e.g. "Electronics",
        /// "Furniture"). The casual Marketplace doesn't enforce a
        /// taxonomy in Phase 1 — chips on the buyer side are derived
        /// from the actual values present.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        public ProductCondition Condition { get; set; }

        /// <summary>South African province (e.g. "Gauteng").</summary>
        public string Province { get; set; } = string.Empty;

        /// <summary>City / area free-text (e.g. "Sandton, Johannesburg").</summary>
        public string Location { get; set; } = string.Empty;

        /// <summary>True when the seller is open to offers below the asking price.</summary>
        public bool AllowOffers { get; set; }

        /// <summary>
        /// Admin-set promotion flags. Default false; no UI for sellers to
        /// flip these directly in Phase 1.
        /// </summary>
        public bool IsBoosted { get; set; }
        public bool IsFeatured { get; set; }

        public MarketplaceListingStatus Status { get; set; } = MarketplaceListingStatus.Active;

        /// <summary>Ordered set of images attached to this listing.</summary>
        public List<MarketplaceListingImage> Images { get; set; } = new();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
