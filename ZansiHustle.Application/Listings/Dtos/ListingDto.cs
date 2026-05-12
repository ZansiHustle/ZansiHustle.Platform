using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Full detail DTO for a single listing. Used by GET /api/listings/{id}.
    /// </summary>
    public class ListingDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public ListingType Type { get; set; }
        public ListingStatus Status { get; set; }

        /// <summary>
        /// Where this listing is available — drives whether it appears
        /// in Home/Explore feeds (OnlineOnly + OnlineAndInStore) or
        /// only on the merchant's Store profile (InStoreOnly).
        /// </summary>
        public AvailabilityMode AvailabilityMode { get; set; }

        /// <summary>Sales channel under which this listing was created.</summary>
        public ListingSource ListingSource { get; set; }

        /// <summary>FK to ShopProfile when ListingSource == ShopProfile; null otherwise.</summary>
        public Guid? ShopProfileId { get; set; }

        /// <summary>Joined display name of the ShopProfile when ListingSource == ShopProfile.</summary>
        public string? ShopProfileName { get; set; }

        public Guid MerchantId { get; set; }
        public string? MerchantName { get; set; }
        public string? MerchantSlug { get; set; }
        public string? MerchantLogoUrl { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZAR";

        public Guid? SellerCategoryId { get; set; }
        public string? SellerCategoryName { get; set; }
        public Guid? SellerSubcategoryId { get; set; }
        public string? SellerSubcategoryName { get; set; }

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
        public List<string>? DeliveryOptions { get; set; }

        // Service-only.
        public PricingModel? PricingModel { get; set; }
        public string? ServiceArea { get; set; }
        public string? Turnaround { get; set; }
        public List<string>? Availability { get; set; }
        public List<string>? BookingMethods { get; set; }

        /// <summary>
        /// Variants for this listing. Empty when the seller hasn't
        /// added any — the listing is sold as a single SKU at
        /// <see cref="Price"/>. Ordered by <c>SortOrder</c>. Public
        /// callers only ever see active variants; the seller-side
        /// edit round-trip surfaces the full set (including inactive)
        /// so it can be re-saved without data loss.
        /// </summary>
        public List<ListingVariantDto> Variants { get; set; } = new();

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
