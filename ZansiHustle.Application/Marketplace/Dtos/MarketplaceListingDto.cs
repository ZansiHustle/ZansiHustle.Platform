using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Marketplace;

namespace ZansiHustle.Application.Marketplace.Dtos
{
    /// <summary>
    /// Buyer-facing read model for a Marketplace listing. Mirrors the mobile
    /// type at <c>features/marketplace/types/MarketplaceListing.ts</c>
    /// field-for-field so the frontend swap from mock to API is a one-line
    /// import change with no mapper layer required.
    /// </summary>
    public class MarketplaceListingDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string Category { get; set; } = string.Empty;
        public ProductCondition Condition { get; set; }
        public List<string> Images { get; set; } = new();
        public string Province { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool AllowOffers { get; set; }

        /// <summary>Display name composed from the seller's profile.</summary>
        public string SellerName { get; set; } = string.Empty;

        /// <summary>True when the seller has confirmed their email.</summary>
        public bool SellerVerified { get; set; }

        /// <summary>
        /// Routing target for buyer-side message threads. The chat
        /// system uses this id (NOT SellerName) to bind the thread to a
        /// real account.
        /// </summary>
        public Guid SellerUserId { get; set; }

        public bool IsBoosted { get; set; }
        public bool IsFeatured { get; set; }

        public MarketplaceListingStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
