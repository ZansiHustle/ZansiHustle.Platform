using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Seller request to update an existing listing.
    /// <see cref="Type"/> and <see cref="MerchantId"/> are immutable after creation
    /// and therefore not present here.
    /// </summary>
    public class UpdateListingRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; } = "ZAR";

        public Guid? SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }

        public List<string>? Images { get; set; }

        public ListingStatus? Status { get; set; }

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
    }
}
