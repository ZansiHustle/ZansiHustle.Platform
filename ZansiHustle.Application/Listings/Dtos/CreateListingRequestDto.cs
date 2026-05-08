using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Seller request to create a new listing.
    /// Ownership is enforced via the JWT user owning <see cref="MerchantId"/>.
    /// </summary>
    public class CreateListingRequestDto
    {
        public Guid MerchantId { get; set; }
        public ListingType Type { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Currency { get; set; } = "ZAR";

        public Guid? SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }

        /// <summary>Image URLs (v1 does not handle uploads — URLs only).</summary>
        public List<string>? Images { get; set; }

        public ListingStatus? Status { get; set; } = ListingStatus.Active;

        /// <summary>
        /// Optional. When omitted, <c>ListingService</c> derives the
        /// default from the owning merchant's type:
        /// <c>PhysicalStore → InStoreOnly</c>, otherwise <c>OnlineOnly</c>.
        /// Sending <see cref="AvailabilityMode.InStoreOnly"/> for an
        /// OnlineStore merchant is rejected.
        /// </summary>
        public AvailabilityMode? AvailabilityMode { get; set; }

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
