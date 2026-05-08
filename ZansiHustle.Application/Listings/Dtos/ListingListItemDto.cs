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

        public Guid MerchantId { get; set; }
        public string? MerchantName { get; set; }
        public string? MerchantSlug { get; set; }

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

        public DateTime CreatedAtUtc { get; set; }
    }
}
