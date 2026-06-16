using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.ZansiDispatch;

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

        // ── Product parcel profile — null fields leave existing values
        // unchanged (same merge pattern as the service fulfilment block). ──
        public ZansiDispatchItemSizeCategory? PackageSizeCategory { get; set; }
        public decimal? PackageWeightKg { get; set; }
        public decimal? PackageLengthCm { get; set; }
        public decimal? PackageWidthCm { get; set; }
        public decimal? PackageHeightCm { get; set; }
        public bool? PackageFragile { get; set; }
        public string? PackageContentsDescription { get; set; }

        // Service-only.
        public PricingModel? PricingModel { get; set; }
        public string? ServiceArea { get; set; }
        public string? Turnaround { get; set; }
        public List<string>? Availability { get; set; }
        public List<string>? BookingMethods { get; set; }

        // ── Service fulfilment (optional; null fields leave existing values
        // unchanged — see UpdateAsync mapping). Validated when Type == Service.
        public ServiceFulfilmentMode? FulfilmentMode { get; set; }
        public bool? AllowsHouseCall { get; set; }
        public bool? AllowsProviderLocation { get; set; }
        public string? ProviderLocationName { get; set; }
        public string? ProviderAddressLine1 { get; set; }
        public string? ProviderAddressLine2 { get; set; }
        public string? ProviderCity { get; set; }
        public string? ProviderProvince { get; set; }
        public string? ProviderPostalCode { get; set; }
        public decimal? ProviderLatitude { get; set; }
        public decimal? ProviderLongitude { get; set; }
        public ServiceTravelFeeType? TravelFeeType { get; set; }
        public decimal? TravelFeePerKm { get; set; }
        public decimal? TravelFeeFlatAmount { get; set; }
        public decimal? FreeTravelRadiusKm { get; set; }
        public decimal? MaxTravelDistanceKm { get; set; }
        public decimal? TravelFeeMinimum { get; set; }
        public decimal? TravelFeeMaximum { get; set; }
        /// <summary>House-call surcharge (extra for house calls). Null leaves
        /// existing unchanged; when set must be ≥ R20 (or 0 for none).</summary>
        public decimal? HouseCallSurchargeAmount { get; set; }
        public int? LeadTimeHours { get; set; }
        public int? BufferMinutes { get; set; }
        /// <summary>Default booking duration (minutes). 15–720 when set; null leaves existing unchanged.</summary>
        public int? EstimatedDurationMinutes { get; set; }

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
