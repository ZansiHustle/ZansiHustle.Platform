namespace ZansiHustle.Application.ZansiDispatch.Dtos
{
    /// <summary>
    /// DB-managed checkout quote-curation policy, surfaced to the ZansiDispatch
    /// portal for ops editing. NOT secrets — these are operational/business knobs
    /// (the env/appsettings layer keeps only credentials + hard safety switches).
    /// </summary>
    public sealed class CheckoutCurationSettingsDto
    {
        /// <summary>CSV allowlist of Courier Guy checkout service codes (e.g. "ECO,LOF"). Empty = allow all.</summary>
        public string AllowedServiceCodes { get; set; } = "ECO";
        /// <summary>Service code shown as "Recommended" + default-selected.</summary>
        public string PreferredServiceCode { get; set; } = "ECO";
        /// <summary>Hide options far above the cheapest valid one (e.g. LSX R560 vs R86).</summary>
        public bool HideExpressOutliers { get; set; } = true;
        /// <summary>An option is an outlier when its fee &gt; cheapest valid × this multiplier.</summary>
        public decimal OutlierMultiplier { get; set; } = 3m;
        /// <summary>Expose ALL raw provider options (no curation). Keep false for normal checkout.</summary>
        public bool ShowAdvancedOptions { get; set; } = false;
        /// <summary>SOFT, admin-only warning threshold (ZAR) — flags high checkout fees for ops
        /// awareness. NEVER blocks a quote. 0 = off.</summary>
        public decimal HighFeeWarningThreshold { get; set; } = 0m;
    }
}
