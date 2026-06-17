using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Domain.ZansiDispatch
{
    /// <summary>
    /// A single delivery option presented for a <see cref="ZansiDispatchQuote"/>
    /// (e.g. "Standard Delivery — R130", "Arrange Collection — R0"). One quote
    /// fans out to several options across providers/service levels. The buyer
    /// selects exactly one before checkout. All timestamps are UTC.
    /// </summary>
    public class ZansiDispatchQuoteOption
    {
        public Guid Id { get; set; }

        public Guid QuoteId { get; set; }
        public ZansiDispatchQuote? Quote { get; set; }

        public ZansiDispatchProviderType ProviderType { get; set; }
        /// <summary>Provider-side quote/rate id, when a real provider returned one.</summary>
        public string? ProviderQuoteReference { get; set; }
        /// <summary>Courier service-level id (used on shipment create as service_level_id).</summary>
        public string? ProviderServiceLevelId { get; set; }
        /// <summary>Courier service-level code (e.g. "ECO", "OVN") — used as service_level_code.</summary>
        public string? ServiceLevelCode { get; set; }
        /// <summary>Human courier service-level name (e.g. "Economy").</summary>
        public string? ServiceLevelName { get; set; }

        public ZansiDispatchServiceLevel ServiceLevel { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? Description { get; set; }

        public decimal QuotedAmount { get; set; }
        /// <summary>VAT portion of the rate, when the provider breaks it out.</summary>
        public decimal? VatAmount { get; set; }
        /// <summary>Provider total (incl. VAT), when supplied.</summary>
        public decimal? TotalAmount { get; set; }
        public string Currency { get; set; } = "ZAR";

        public int? EstimatedDeliveryDaysMin { get; set; }
        public int? EstimatedDeliveryDaysMax { get; set; }

        /// <summary>JSON breakdown of how the amount was computed (base/location/size/buffer).</summary>
        public string? EstimateBreakdownJson { get; set; }
        /// <summary>Raw provider response payload, when a real provider was called.</summary>
        public string? RawProviderResponseJson { get; set; }

        public bool IsSelected { get; set; }

        // ── Checkout curation (marketplace-clean choices) ───────────────────
        /// <summary>True when this option may be shown to the CUSTOMER at checkout
        /// (and selected). Raw provider options that are filtered out by policy
        /// (disallowed code / too expensive / express outlier) are kept in the DB
        /// for ops + diagnostics but have this set false. Defaults true (non-courier
        /// + uncurated options).</summary>
        public bool IsCheckoutVisible { get; set; } = true;
        /// <summary>True for the single "Recommended delivery" option (preferred code,
        /// else cheapest visible). Drives the default selection at checkout.</summary>
        public bool IsRecommended { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
