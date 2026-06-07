using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Domain.ZansiDispatch
{
    /// <summary>
    /// A checkout delivery-quote session. Created when the mobile app asks
    /// ZansiDispatch for delivery options; holds the request context and owns
    /// the set of <see cref="ZansiDispatchQuoteOption"/> rows presented to the
    /// buyer. Reference ids (OrderId / ListingId / ShopId / MerchantId) are
    /// loose, un-FK'd Guids — this is an operational logistics record that must
    /// survive deletion of the entities it points at. All timestamps are UTC.
    /// </summary>
    public class ZansiDispatchQuote
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid? OrderId { get; set; }
        public Guid? ListingId { get; set; }
        public Guid? ShopId { get; set; }
        /// <summary>The seller this quote is for — a <c>Merchant</c> id.</summary>
        public Guid? MerchantId { get; set; }

        // ── Buyer (delivery) address ─────────────────────────────────────
        public string? BuyerProvince { get; set; }
        public string? BuyerCity { get; set; }
        public string? BuyerAddressSummary { get; set; }
        public string? BuyerStreetAddress { get; set; }
        public string? BuyerLocalArea { get; set; }
        public string? BuyerPostalCode { get; set; }
        public string? BuyerCountry { get; set; } = "ZA";
        public decimal? BuyerLat { get; set; }
        public decimal? BuyerLng { get; set; }
        public ZansiDispatchAddressType? BuyerAddressType { get; set; }

        // ── Seller (collection) address ──────────────────────────────────
        public string? SellerProvince { get; set; }
        public string? SellerCity { get; set; }
        public string? SellerAddressSummary { get; set; }
        public string? SellerStreetAddress { get; set; }
        public string? SellerLocalArea { get; set; }
        public string? SellerPostalCode { get; set; }
        public string? SellerCountry { get; set; } = "ZA";
        public decimal? SellerLat { get; set; }
        public decimal? SellerLng { get; set; }
        public ZansiDispatchAddressType? SellerAddressType { get; set; }

        // ── Parcel ───────────────────────────────────────────────────────
        public string? ParcelDescription { get; set; }
        public ZansiDispatchItemSizeCategory? ItemSizeCategory { get; set; }
        public decimal? EstimatedWeightKg { get; set; }
        public decimal? SubmittedLengthCm { get; set; }
        public decimal? SubmittedWidthCm { get; set; }
        public decimal? SubmittedHeightCm { get; set; }
        public decimal? DistanceKm { get; set; }
        public decimal? DeclaredValue { get; set; }

        public ZansiDispatchQuoteStatus Status { get; set; } = ZansiDispatchQuoteStatus.Draft;

        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<ZansiDispatchQuoteOption> Options { get; set; } = new();
    }
}
