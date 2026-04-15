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

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
