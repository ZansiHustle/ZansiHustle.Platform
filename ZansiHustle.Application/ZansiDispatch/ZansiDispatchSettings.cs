using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch
{
    /// <summary>
    /// A resolved, strongly-typed snapshot of ZansiDispatch tuning knobs for a
    /// single quote/operation — produced by <c>ZansiDispatchDefaults.Resolve</c>
    /// from DB <c>ZansiDispatchSettings</c> rows with code-default fallback.
    /// </summary>
    public sealed class ZansiDispatchSettings
    {
        public ZansiDispatchProviderType DefaultProvider { get; set; } = ZansiDispatchProviderType.CourierGuy;
        /// <summary>Fall back to the deterministic InternalEstimate when the default provider can't quote.</summary>
        public bool FallbackToInternalEstimate { get; set; } = true;
        /// <summary>Legacy alias kept for back-compat with the original seed; honoured if FallbackToInternalEstimate is absent.</summary>
        public bool AllowManualFallback { get; set; } = true;

        // Standard delivery location components.
        public decimal BaseFee { get; set; } = 80m;
        public decimal SameCityFee { get; set; } = 30m;
        public decimal DifferentCityFee { get; set; } = 60m;
        public decimal DifferentProvinceFee { get; set; } = 100m;

        // Parcel-size components.
        public decimal SmallFee { get; set; } = 0m;
        public decimal MediumFee { get; set; } = 30m;
        public decimal LargeFee { get; set; } = 70m;
        public decimal ExtraLargeFee { get; set; } = 120m;

        public decimal RiskBuffer { get; set; } = 20m;
        public decimal MinimumFee { get; set; } = 80m;
        public decimal MaximumNormalFee { get; set; } = 350m;

        // Courier Guy rates expire after ~5 minutes — keep quotes short-lived.
        public int QuoteExpiryMinutes { get; set; } = 5;
        public bool CollectionEnabled { get; set; } = true;

        // ── Checkout quote curation (DB-managed, ops-editable) ───────────────
        // Business/operational policy — NOT secrets. Edited from the ZansiDispatch
        // portal; never an env var. Curates raw Courier Guy rate codes into clean
        // customer choices. Raw options are always persisted for ops/diagnostics.
        /// <summary>CSV allowlist of checkout service codes (e.g. "ECO,LOF"). Empty = allow all (then outlier filter).</summary>
        public string CheckoutAllowedServiceCodes { get; set; } = "ECO";
        /// <summary>Service code shown as "Recommended" + default-selected.</summary>
        public string CheckoutPreferredServiceCode { get; set; } = "ECO";
        /// <summary>Hide options far above the cheapest valid one (e.g. LSX R560 vs R86).</summary>
        public bool CheckoutHideExpressOutliers { get; set; } = true;
        /// <summary>An option is an outlier when its fee &gt; cheapest valid × this multiplier.</summary>
        public decimal CheckoutOutlierMultiplier { get; set; } = 3m;
        /// <summary>When true, expose ALL raw provider options (no curation). Default false.</summary>
        public bool CheckoutShowAdvancedOptions { get; set; } = false;
        /// <summary>SOFT, admin-visible-only threshold: flag (NOT block) checkout fees above
        /// this (ZAR) for ops awareness. 0 = no warning. NEVER blocks a valid quote.</summary>
        public decimal CheckoutHighFeeWarningThreshold { get; set; } = 0m;

        /// <summary>Parsed, case-insensitive allowed-code set. Empty = allow all.</summary>
        public System.Collections.Generic.HashSet<string> CheckoutAllowedCodeSet()
        {
            var set = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(CheckoutAllowedServiceCodes)) return set;
            foreach (var c in CheckoutAllowedServiceCodes.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries))
                set.Add(c);
            return set;
        }

        /// <summary>Per-size fee lookup.</summary>
        public decimal SizeFee(ZansiDispatchItemSizeCategory size) => size switch
        {
            ZansiDispatchItemSizeCategory.Small => SmallFee,
            ZansiDispatchItemSizeCategory.Medium => MediumFee,
            ZansiDispatchItemSizeCategory.Large => LargeFee,
            ZansiDispatchItemSizeCategory.ExtraLarge => ExtraLargeFee,
            _ => MediumFee,
        };
    }
}
