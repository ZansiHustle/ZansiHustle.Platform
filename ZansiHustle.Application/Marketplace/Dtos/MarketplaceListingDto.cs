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

        /// <summary>
        /// Per-image detail (id + url + sortOrder). Same set + ordering
        /// as <see cref="Images"/>; consumed by the owner-edit screen so
        /// each image can be referenced by id for delete / reorder.
        /// </summary>
        public List<MarketplaceListingImageDto> ImageItems { get; set; } = new();
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

        // ── Engagement ───────────────────────────────────────────────
        /// <summary>Denormalised heart count from <c>MarketplaceListing.LikeCount</c>.</summary>
        public int LikeCount { get; set; }
        /// <summary>
        /// True when the authenticated caller has liked this casual
        /// listing. Always <c>false</c> for anonymous / unauthenticated
        /// reads.
        /// </summary>
        public bool IsLikedByMe { get; set; }

        // ── Reviews (denormalised aggregate) ─────────────────────────
        /// <summary>
        /// Average review rating (1.00–5.00) over Active reviews for this
        /// listing, or null when there are none. Mirrors
        /// <c>MarketplaceListing.Rating</c> so the buyer detail screen can show
        /// the aggregate without a second /api/reviews/summary call.
        /// </summary>
        public decimal? Rating { get; set; }

        /// <summary>Count of Active reviews for this listing.</summary>
        public int ReviewCount { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
