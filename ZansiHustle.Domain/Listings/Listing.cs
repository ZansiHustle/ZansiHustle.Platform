using System;
using System.Collections.Generic;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Domain.Listings
{
    /// <summary>
    /// Unified product / service listing owned by a <see cref="Merchant"/>.
    /// Type-specific fields (e.g. Stock, PricingModel) are nullable and only
    /// populated for the corresponding <see cref="ListingType"/>.
    /// </summary>
    public class Listing
    {
        public Guid Id { get; set; }

        /// <summary>Short opaque business code (e.g. <c>LIS-20260415...</c>).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>URL-safe unique slug derived from the title at creation.</summary>
        public string Slug { get; set; } = string.Empty;

        public ListingType Type { get; set; }
        public ListingStatus Status { get; set; } = ListingStatus.Active;

        /// <summary>
        /// Where this listing is available — gates which surfaces may
        /// render it (Home / Explore feeds vs. Store profile only) and
        /// whether online checkout applies. Defaults to
        /// <see cref="AvailabilityMode.OnlineOnly"/> at the database
        /// level (migration backfill); <c>ListingService</c> overrides
        /// the default at create-time based on the owning merchant's
        /// type (<c>PhysicalStore → InStoreOnly</c>).
        /// </summary>
        public AvailabilityMode AvailabilityMode { get; set; } = AvailabilityMode.OnlineOnly;

        /// <summary>Owning merchant. Required — a listing always belongs to a merchant.</summary>
        public Guid MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        /// <summary>
        /// The sales channel this listing was created under. Decides
        /// which buyer surfaces render it and what display-owner pill
        /// the card shows. See <see cref="ListingSource"/> for the
        /// per-value contract. Defaults to SellerAccount so legacy
        /// rows (created before this column existed) still appear in
        /// the seller's My Listings without dragging into a shop they
        /// were never listed under.
        /// </summary>
        public ListingSource ListingSource { get; set; } = ListingSource.SellerAccount;

        /// <summary>
        /// FK to <see cref="ShopProfile"/> when (and only when)
        /// <see cref="ListingSource"/> is <c>ShopProfile</c>. The
        /// ShopProfile's <c>MerchantId</c> MUST equal this listing's
        /// <c>MerchantId</c> — same merchant owns both. Buyer-facing
        /// shop catalog filters by THIS column (not by MerchantId) so
        /// SellerAccount listings stay out of the shop's catalog.
        /// </summary>
        public Guid? ShopProfileId { get; set; }
        public ShopProfile? ShopProfile { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Unit price. For services with PricingModel.Quote/Negotiable this may be 0.</summary>
        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZAR";

        public Guid? SellerCategoryId { get; set; }
        public SellerCategory? SellerCategory { get; set; }

        public Guid? SellerSubcategoryId { get; set; }
        public SellerSubcategory? SellerSubcategory { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }

        /// <summary>Ordered list of image URLs (v1 stores URLs only — no upload).</summary>
        public List<string> Images { get; set; } = new();

        public bool IsFeatured { get; set; }
        public bool IsBoosted { get; set; }

        // Aggregate review metrics (no per-review entity yet).
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }

        /// <summary>
        /// Denormalised count of buyer "hearts" / likes on this listing.
        /// Source of truth is the <c>ListingLikes</c> table; the
        /// engagement service updates this column inside the same
        /// transaction as the like-row insert/delete so reads don't
        /// have to aggregate per row.
        /// </summary>
        public int LikeCount { get; set; }

        // Product-only fields.
        public int? Stock { get; set; }
        public ListingCondition? Condition { get; set; }
        public List<string>? DeliveryOptions { get; set; }

        // Service-only fields.
        public PricingModel? PricingModel { get; set; }
        public string? ServiceArea { get; set; }
        public string? Turnaround { get; set; }
        public List<string>? Availability { get; set; }
        public List<string>? BookingMethods { get; set; }

        // ── Service fulfilment (house call / provider location / both) ──────
        // All nullable + additive: existing service rows keep null until the
        // seller configures fulfilment, and the buyer flow falls back safely.
        // Product listings leave every field null. See ServiceFulfilmentMode /
        // ServiceTravelFeeType.
        public ServiceFulfilmentMode? FulfilmentMode { get; set; }
        public bool? AllowsHouseCall { get; set; }
        public bool? AllowsProviderLocation { get; set; }

        // Provider's per-service location (NOT auto-copied from the merchant
        // onboarding address — the seller confirms it per service).
        public string? ProviderLocationName { get; set; }
        public string? ProviderAddressLine1 { get; set; }
        public string? ProviderAddressLine2 { get; set; }
        public string? ProviderCity { get; set; }
        public string? ProviderProvince { get; set; }
        public string? ProviderPostalCode { get; set; }
        public decimal? ProviderLatitude { get; set; }
        public decimal? ProviderLongitude { get; set; }

        // Travel-fee configuration (house call only).
        public ServiceTravelFeeType? TravelFeeType { get; set; }
        public decimal? TravelFeePerKm { get; set; }
        public decimal? TravelFeeFlatAmount { get; set; }
        public decimal? FreeTravelRadiusKm { get; set; }
        public decimal? MaxTravelDistanceKm { get; set; }
        public decimal? TravelFeeMinimum { get; set; }
        public decimal? TravelFeeMaximum { get; set; }

        /// <summary>
        /// Extra charged on top of <see cref="Price"/> when the buyer chooses a
        /// HOUSE CALL (covers the provider's travel time/effort). Provider-location
        /// bookings never pay it. Collected upfront by the platform together with
        /// the service fee + travel fee; the provider is credited later from the
        /// platform balance. Null/0 = no surcharge; when set it must be ≥ R20
        /// (the product minimum). Independent of <see cref="TravelFeeType"/>.
        /// </summary>
        public decimal? HouseCallSurchargeAmount { get; set; }

        // Scheduling hints (optional, reserved for availability work).
        public int? LeadTimeHours { get; set; }
        public int? BufferMinutes { get; set; }

        /// <summary>
        /// Seller-set default time a single booking of this service takes, in
        /// minutes. Drives availability slot length + calendar blocking (a 3h
        /// service booked at 14:00 blocks 14:00–17:00). Nullable: legacy services
        /// fall back to a 60-minute default. Validated 15–720 when set (services
        /// only).
        /// </summary>
        public int? EstimatedDurationMinutes { get; set; }

        // ── Product delivery / parcel profile (platform-managed courier) ────
        // All nullable + additive: existing product rows + every service row
        // stay null. A product is courier-bookable via ZansiDispatch only when
        // weight + all three dimensions are positive (see IsParcelComplete).
        // The seller form prefills these from a size preset; "Custom" lets the
        // seller enter exact values. Consumed by the ZansiDispatch quote flow
        // (accurate Courier Guy rates) and the create-from-quote booking guard.
        public ZansiDispatchItemSizeCategory? PackageSizeCategory { get; set; }
        public decimal? PackageWeightKg { get; set; }
        public decimal? PackageLengthCm { get; set; }
        public decimal? PackageWidthCm { get; set; }
        public decimal? PackageHeightCm { get; set; }
        public bool? PackageFragile { get; set; }
        public string? PackageContentsDescription { get; set; }

        /// <summary>
        /// True when this listing carries a complete parcel profile (weight +
        /// all three dimensions positive) — i.e. it can be quoted accurately
        /// and booked with a courier. Products without it must not reach
        /// courier delivery checkout. Always false for services.
        /// </summary>
        public bool IsParcelComplete =>
            Type == ListingType.Product
            && PackageWeightKg is > 0m
            && PackageLengthCm is > 0m
            && PackageWidthCm is > 0m
            && PackageHeightCm is > 0m;

        /// <summary>
        /// Child variants (colour / size / storage / package options).
        /// Empty list = "no variants" — the listing is sold as a single
        /// SKU with this row's <see cref="Price"/> + <see cref="Stock"/>.
        /// Variants are cascade-deleted with the parent listing.
        /// </summary>
        public List<ListingVariant> Variants { get; set; } = new();

        // ── External catalog sync (mirrored from an approved external source, e.g. ZansiTech) ──
        // Null ExternalSourceCode = a normal, manually-managed ZansiHustle listing (the
        // overwhelming majority). Non-null = externally managed: ListingService.UpdateAsync
        // rejects ordinary seller edits to these rows — the source system is authoritative
        // and the next sync always wins on these fields.
        /// <summary>Lowercase source code (e.g. "zansitech"). Null = not externally managed.</summary>
        public string? ExternalSourceCode { get; set; }

        /// <summary>The source system's own stable Product identifier. Combined with ExternalSourceCode, uniquely identifies this mirrored Listing.</summary>
        public string? ExternalProductId { get; set; }

        /// <summary>The source-side timestamp/version last applied. Stale-update protection: an incoming sync with an older value than this is ignored.</summary>
        public DateTime? ExternalSourceUpdatedAtUtc { get; set; }

        /// <summary>When ZansiHustle itself last wrote this row from a sync (audit/observability — distinct from ExternalSourceUpdatedAtUtc, which is the source's own clock).</summary>
        public DateTime? ExternalSyncedAtUtc { get; set; }

        /// <summary>Which full-snapshot run last touched this row. Used as the archive-boundary marker — see ExternalCatalogSyncRun.</summary>
        public Guid? ExternalSyncRunId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
