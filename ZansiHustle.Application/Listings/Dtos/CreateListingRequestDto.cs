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

        /// <summary>
        /// Sales channel the listing is being created under. Optional
        /// for back-compat: when omitted the service infers from the
        /// owning merchant's type (PhysicalStore → PhysicalStore,
        /// otherwise SellerAccount). Sellers with a ShopProfile MUST
        /// send <c>ShopProfile</c> AND <see cref="ShopProfileId"/> to
        /// list under the shop; otherwise the listing falls into the
        /// seller's bare account and won't surface on the shop page.
        /// </summary>
        public ListingSource? ListingSource { get; set; }

        /// <summary>
        /// FK to the caller's ShopProfile. REQUIRED when
        /// <see cref="ListingSource"/> is <c>ShopProfile</c>; MUST be
        /// null otherwise. The shop's <c>MerchantId</c> is validated
        /// against the request's <see cref="MerchantId"/> server-side
        /// so a seller can't list under someone else's shop.
        /// </summary>
        public Guid? ShopProfileId { get; set; }

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

        // ── Service fulfilment (optional; validated when Type == Service) ────
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
        /// <summary>House-call surcharge (extra on top of the service price for
        /// house calls). Null/0 = none; when set must be ≥ R20.</summary>
        public decimal? HouseCallSurchargeAmount { get; set; }
        public int? LeadTimeHours { get; set; }
        public int? BufferMinutes { get; set; }
        /// <summary>Default booking duration (minutes). 15–720 when set; null → 60 fallback.</summary>
        public int? EstimatedDurationMinutes { get; set; }

        /// <summary>
        /// Optional flat list of variants (colour / size / storage /
        /// package). Omit or send empty → "no variants", listing
        /// behaves as a single SKU. Variants without
        /// <c>UsesCustomPrice = true</c> inherit the listing's
        /// <see cref="Price"/>. Capped at 20 entries by the service.
        /// </summary>
        public List<ListingVariantRequestDto>? Variants { get; set; }
    }
}
