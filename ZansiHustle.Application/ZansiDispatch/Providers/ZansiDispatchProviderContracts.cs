using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch.Providers
{
    /// <summary>Everything a quote provider needs to price delivery. All fields optional/defensive.</summary>
    public sealed class ZansiDispatchQuoteContext
    {
        public Guid? ListingId { get; set; }
        public Guid? ShopId { get; set; }
        public Guid? MerchantId { get; set; }

        // ── Buyer (delivery) address ─────────────────────────────────────
        public string? BuyerCompany { get; set; }
        public ZansiDispatchAddressType? BuyerAddressType { get; set; }
        public string? BuyerStreetAddress { get; set; }
        public string? BuyerLocalArea { get; set; }
        public string? BuyerCity { get; set; }
        public string? BuyerProvince { get; set; }
        public string? BuyerCountry { get; set; }
        public string? BuyerPostalCode { get; set; }
        public decimal? BuyerLat { get; set; }
        public decimal? BuyerLng { get; set; }
        public string? BuyerAddressSummary { get; set; }

        // ── Seller (collection) address ──────────────────────────────────
        public string? SellerCompany { get; set; }
        public ZansiDispatchAddressType? SellerAddressType { get; set; }
        public string? SellerStreetAddress { get; set; }
        public string? SellerLocalArea { get; set; }
        public string? SellerCity { get; set; }
        public string? SellerProvince { get; set; }
        public string? SellerCountry { get; set; }
        public string? SellerPostalCode { get; set; }
        public decimal? SellerLat { get; set; }
        public decimal? SellerLng { get; set; }
        public string? SellerAddressSummary { get; set; }

        // ── Parcel ───────────────────────────────────────────────────────
        public string? ParcelDescription { get; set; }
        public ZansiDispatchItemSizeCategory? ItemSizeCategory { get; set; }
        public decimal? EstimatedWeightKg { get; set; }
        public decimal? SubmittedLengthCm { get; set; }
        public decimal? SubmittedWidthCm { get; set; }
        public decimal? SubmittedHeightCm { get; set; }
        public decimal? DistanceKm { get; set; }
        public decimal? DeclaredValue { get; set; }

        public DateTime? CollectionMinDate { get; set; }
        public DateTime? DeliveryMinDate { get; set; }
    }

    /// <summary>One delivery option produced by a provider, before persistence.</summary>
    public sealed class ProviderQuoteOption
    {
        public ZansiDispatchProviderType ProviderType { get; set; }
        public string? ProviderQuoteReference { get; set; }
        public string? ProviderServiceLevelId { get; set; }
        public string? ServiceLevelCode { get; set; }
        public string? ServiceLevelName { get; set; }
        public ZansiDispatchServiceLevel ServiceLevel { get; set; } = ZansiDispatchServiceLevel.Standard;
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal QuotedAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public int? EstimatedDeliveryDaysMin { get; set; }
        public int? EstimatedDeliveryDaysMax { get; set; }
        public string? EstimateBreakdownJson { get; set; }
        public string? RawProviderResponseJson { get; set; }
    }

    /// <summary>
    /// Provider quote result + the raw HTTP envelope (request/response/status) so
    /// the service can persist an accurate, secret-masked provider-request log.
    /// </summary>
    public sealed class ProviderQuoteResult
    {
        public List<ProviderQuoteOption> Options { get; set; } = new();
        /// <summary>True when the provider call genuinely succeeded with usable options.</summary>
        public bool Ok { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? RawRequestJson { get; set; }
        public string? RawResponseJson { get; set; }
        public int? StatusCode { get; set; }
    }

    // ── Shipment provider — structured request/result ───────────────────────

    public sealed class ProviderAddress
    {
        public string? Company { get; set; }
        public ZansiDispatchAddressType? Type { get; set; }
        public string? StreetAddress { get; set; }
        public string? LocalArea { get; set; }
        public string? City { get; set; }
        public string? Zone { get; set; }       // province
        public string? Country { get; set; }    // ISO-2, default ZA
        public string? Code { get; set; }        // postal code
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
    }

    public sealed class ProviderContact
    {
        public string? Name { get; set; }
        public string? MobileNumber { get; set; }
        public string? Email { get; set; }
    }

    public sealed class ProviderParcel
    {
        public string? Description { get; set; }
        public decimal? SubmittedLengthCm { get; set; }
        public decimal? SubmittedWidthCm { get; set; }
        public decimal? SubmittedHeightCm { get; set; }
        public decimal? SubmittedWeightKg { get; set; }
    }

    public sealed class ProviderShipmentRequest
    {
        public Guid ShipmentId { get; set; }
        public Guid OrderId { get; set; }

        public ProviderAddress CollectionAddress { get; set; } = new();
        public ProviderContact CollectionContact { get; set; } = new();
        public ProviderAddress DeliveryAddress { get; set; } = new();
        public ProviderContact DeliveryContact { get; set; } = new();
        public List<ProviderParcel> Parcels { get; set; } = new();

        /// <summary>Either ServiceLevelCode or ServiceLevelId from the chosen rate.</summary>
        public string? ServiceLevelCode { get; set; }
        public string? ServiceLevelId { get; set; }

        public string? CustomerReference { get; set; }
        public string? CustomerReferenceName { get; set; }
        public string? SpecialInstructionsCollection { get; set; }
        public string? SpecialInstructionsDelivery { get; set; }
        public bool MuteNotifications { get; set; }
        public decimal? DeclaredValue { get; set; }
        public DateTime? CollectionMinDate { get; set; }
        public DateTime? DeliveryMinDate { get; set; }
    }

    public sealed class ProviderShipmentResult
    {
        public bool Ok { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? ProviderShipmentId { get; set; }
        public string? ProviderShipmentReference { get; set; }
        public string? TrackingNumber { get; set; }
        public string? ShortTrackingReference { get; set; }
        public string? ServiceLevelCode { get; set; }
        public string? ServiceLevelName { get; set; }
        public decimal? BookedCost { get; set; }
        /// <summary>Provider base rate (before adjustments), if returned.</summary>
        public decimal? BaseRate { get; set; }
        public string? InitialProviderStatus { get; set; }
        public ZansiDispatchShipmentStatus? InitialStatus { get; set; }

        // ── Courier date promises + parcel facts (only set when returned) ───
        public DateTime? ExpectedCollectionDate { get; set; }
        public DateTime? ExpectedDeliveryFrom { get; set; }
        public DateTime? ExpectedDeliveryTo { get; set; }
        public decimal? ChargedWeightKg { get; set; }
        public decimal? ActualWeightKg { get; set; }
        public decimal? VolumetricWeightKg { get; set; }
        public string? ProviderStatusMessage { get; set; }
        public string? PackageTrackingReference { get; set; }

        public string? RawRequestJson { get; set; }
        public string? RawResponseJson { get; set; }
        public int? StatusCode { get; set; }
    }

    /// <summary>One mapped tracking event from a provider poll or webhook.</summary>
    public sealed class ProviderTrackingEvent
    {
        public string ProviderStatus { get; set; } = string.Empty;
        public ZansiDispatchShipmentStatus InternalStatus { get; set; }
        public string? Message { get; set; }
        public string? Location { get; set; }
        public DateTime EventTime { get; set; }
        public string? ProviderEventId { get; set; }
        public string? RawJson { get; set; }
    }

    public sealed class ProviderTrackingResult
    {
        public bool Ok { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public ZansiDispatchShipmentStatus? CurrentStatus { get; set; }
        public string? CurrentProviderStatus { get; set; }
        /// <summary>Latest human-readable status message (e.g. "A driver has been allocated…").</summary>
        public string? CurrentStatusMessage { get; set; }
        public List<ProviderTrackingEvent> Events { get; set; } = new();

        // ── Updated courier date promises from the tracking poll (when present) ──
        public DateTime? ExpectedCollectionDate { get; set; }
        public DateTime? ExpectedDeliveryFrom { get; set; }
        public DateTime? ExpectedDeliveryTo { get; set; }

        public string? RawResponseJson { get; set; }
        public int? StatusCode { get; set; }
    }

    public sealed class ProviderCancelResult
    {
        public bool Ok { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? RawResponseJson { get; set; }
        public int? StatusCode { get; set; }
    }

    public sealed class ProviderLabelResult
    {
        public bool Ok { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? LabelUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? RawResponseJson { get; set; }
        public int? StatusCode { get; set; }
    }

    /// <summary>Result of a pickup/delivery reschedule attempt.</summary>
    public sealed class ProviderRescheduleResult
    {
        public bool Ok { get; set; }
        /// <summary>False when the provider integration doesn't support rescheduling
        /// (the service then creates an ops task rather than faking success).</summary>
        public bool Supported { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RawResponseJson { get; set; }
        public int? StatusCode { get; set; }
    }
}
