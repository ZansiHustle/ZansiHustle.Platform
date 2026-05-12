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

        /// <summary>
        /// Optional. When supplied, the same merchant-type validation
        /// applies as on create — InStoreOnly requires PhysicalStore.
        /// Omitting leaves the existing value unchanged.
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

        /// <summary>
        /// Variant set after the update. Omit / send null to leave the
        /// existing variants untouched. Send an empty list to clear all
        /// variants. Diff semantics: variants with a matching <c>Id</c>
        /// are updated in place; rows without an <c>Id</c> (or with one
        /// that doesn't match an existing variant of this listing) are
        /// inserted; existing variants absent from the request are
        /// removed.
        /// </summary>
        public List<ListingVariantRequestDto>? Variants { get; set; }
    }
}
