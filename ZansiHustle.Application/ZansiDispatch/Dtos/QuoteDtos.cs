using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch.Dtos
{
    /// <summary>
    /// Request for <c>POST /api/zansidispatch/quotes</c>. Mobile checkout sends
    /// the buyer's location + (ideally) the listing/merchant so the backend can
    /// resolve the seller's location and price delivery. Backend is the source
    /// of truth — the client never computes the fee.
    /// </summary>
    public sealed class CreateQuoteRequestDto
    {
        public Guid? ListingId { get; set; }
        public Guid? ShopId { get; set; }
        /// <summary>Seller merchant id. <c>SellerId</c> is accepted as an alias.</summary>
        public Guid? MerchantId { get; set; }
        public Guid? SellerId { get; set; }

        // ── Buyer (delivery) address ─ all optional; richer fields let the
        //    courier provider quote accurately (postal code is required by CG). ─
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

        /// <summary>Include the free "Arrange Collection" option (when enabled). Defaults to true.</summary>
        public bool? IncludeCollectionOption { get; set; }
    }

    public sealed class QuoteOptionDto
    {
        public Guid QuoteOptionId { get; set; }
        public ZansiDispatchProviderType ProviderType { get; set; }
        public string? ProviderQuoteReference { get; set; }
        public string? ProviderServiceLevelId { get; set; }
        public string? ServiceLevelCode { get; set; }
        public string? ServiceLevelName { get; set; }
        public ZansiDispatchServiceLevel ServiceLevel { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal QuotedAmount { get; set; }
        public decimal? VatAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public int? EstimatedDeliveryDaysMin { get; set; }
        public int? EstimatedDeliveryDaysMax { get; set; }
        /// <summary>Raw JSON breakdown of the estimate (how the amount was computed).</summary>
        public string? EstimateBreakdown { get; set; }
        public bool IsSelected { get; set; }
        /// <summary>True for the single curated "Recommended delivery" option —
        /// the checkout default-selects this.</summary>
        public bool IsRecommended { get; set; }
    }

    public sealed class QuoteDto
    {
        public Guid QuoteId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public ZansiDispatchQuoteStatus Status { get; set; }
        /// <summary>Which provider produced the priced options.</summary>
        public ZansiDispatchProviderType ProviderUsed { get; set; }
        /// <summary>True when the default provider couldn't quote and InternalEstimate was used.</summary>
        public bool FallbackUsed { get; set; }
        /// <summary>
        /// Admin/dev-facing pricing source label (e.g. "CourierGuy",
        /// "InternalEstimate", "InternalEstimate (fallback)"). For ops dashboards
        /// + manual-quote diagnostics — NOT shown to buyers (the mobile checkout
        /// renders only the option label/description).
        /// </summary>
        public string PricingSource { get; set; } = string.Empty;
        public List<QuoteOptionDto> Options { get; set; } = new();
    }
}
