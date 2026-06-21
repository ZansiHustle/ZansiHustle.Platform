using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Lightweight DTO used in list endpoints. Omits long prose and secondary
    /// collections to keep buyer-facing pages snappy.
    /// </summary>
    public class ListingListItemDto
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;

        public ListingType Type { get; set; }
        public ListingStatus Status { get; set; }

        /// <summary>
        /// Where this listing is available. Mobile feed clients hide
        /// online-only CTAs (Add to cart) and show "Available in store"
        /// for InStoreOnly / OnlineAndInStore items on Store surfaces.
        /// </summary>
        public AvailabilityMode AvailabilityMode { get; set; }

        /// <summary>Sales channel under which this listing was created.</summary>
        public ListingSource ListingSource { get; set; }

        /// <summary>FK to ShopProfile when ListingSource == ShopProfile; null otherwise.</summary>
        public Guid? ShopProfileId { get; set; }

        /// <summary>Joined ShopProfile name when ListingSource == ShopProfile.</summary>
        public string? ShopProfileName { get; set; }

        /// <summary>Joined ShopProfile LOGO (the storefront's own brand mark)
        /// when ListingSource == ShopProfile; null otherwise. Distinct from the
        /// owning Merchant's logo — a shop-attached listing's card pill should
        /// show THIS, not the merchant logo.</summary>
        public string? ShopLogoUrl { get; set; }

        public Guid MerchantId { get; set; }
        public string? MerchantName { get; set; }
        public string? MerchantSlug { get; set; }

        /// <summary>Shop/brand logo (public). Used by feed cards for the seller
        /// pill avatar when the merchant presents as a store.</summary>
        public string? MerchantLogoUrl { get; set; }

        /// <summary>Seller/provider PUBLIC profile picture (the face shown to
        /// buyers). Distinct from the shop logo and from the private KYC selfie.
        /// Feed cards prefer this for the seller pill avatar.</summary>
        public string? MerchantProfileImageUrl { get; set; }

        /// <summary>True when the owning merchant is KYC-verified. Drives the
        /// real verified tick on feed cards — never fabricated.</summary>
        public bool MerchantVerified { get; set; }

        /// <summary>True when the signed-in user owns this listing — drives the
        /// "Your listing" marker on feed cards. False for anonymous callers.</summary>
        public bool IsOwner { get; set; }

        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZAR";

        public Guid? SellerCategoryId { get; set; }
        public string? SellerCategoryName { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }

        public List<string> Images { get; set; } = new();

        public bool IsFeatured { get; set; }
        public bool IsBoosted { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }

        // Product-only.
        public int? Stock { get; set; }
        public ListingCondition? Condition { get; set; }

        // Service-only.
        public PricingModel? PricingModel { get; set; }

        /// <summary>
        /// Number of active variants attached to this listing. Cards
        /// can decorate themselves with a "Variants available" hint
        /// without needing to ship the full variant collection in the
        /// list response.
        /// </summary>
        public int VariantCount { get; set; }

        // ── Engagement ───────────────────────────────────────────────
        /// <summary>Denormalised heart count from <c>Listing.LikeCount</c>.</summary>
        public int LikeCount { get; set; }
        /// <summary>
        /// True when the authenticated caller has liked this listing.
        /// Always <c>false</c> for anonymous / unauthenticated reads.
        /// </summary>
        public bool IsLikedByMe { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
