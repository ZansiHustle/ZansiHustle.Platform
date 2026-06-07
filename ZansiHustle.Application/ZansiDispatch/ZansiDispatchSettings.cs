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
