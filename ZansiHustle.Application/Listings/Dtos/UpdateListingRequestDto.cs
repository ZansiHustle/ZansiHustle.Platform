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
        /// Variant set after the update.
        ///
        ///   • <c>null</c> / omitted → existing variants untouched.
        ///   • <c>[]</c>             → all existing variants are deleted.
        ///   • non-empty             → existing variants are REPLACED
        ///                             wholesale with this list.
        ///
        /// REPLACE semantics, NOT a diff. Any <c>Id</c> field on an
        /// incoming variant is ignored — the server issues fresh PKs
        /// for every row. The previous diff-by-id strategy produced
        /// false <c>DbUpdateConcurrencyException</c>s on normal edits
        /// when the tracker / navigation-collection state got tangled
        /// between matched-Modified and Added rows in the same
        /// SaveChanges. Wholesale replace is atomic (single transaction)
        /// and tracker-free.
        ///
        /// Trade-off: variant ids are not stable across an update.
        /// Acceptable today — no order / cart / wishlist references
        /// variant ids yet. Buyers always refetch listing detail after
        /// a seller save, so the new ids land naturally.
        /// </summary>
        public List<ListingVariantRequestDto>? Variants { get; set; }
    }
}
