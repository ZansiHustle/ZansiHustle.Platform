using System;
using System.Collections.Generic;
using System.Globalization;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch
{
    /// <summary>
    /// Code-default values + keys for every ZansiDispatch setting. These are
    /// the fallback used whenever a matching <c>ZansiDispatchSetting</c> row is
    /// absent or inactive, so quoting always has sane numbers even before (or
    /// instead of) database tuning. The seeder writes these same values into
    /// <c>ZansiDispatchSettings</c> on first run.
    /// </summary>
    public static class ZansiDispatchDefaults
    {
        // ── Keys ────────────────────────────────────────────────────────────
        public const string DefaultProviderKey = "Dispatch.DefaultProvider";
        public const string FallbackToInternalEstimateKey = "Dispatch.FallbackToInternalEstimate";
        public const string AllowManualFallbackKey = "Dispatch.AllowManualFallback";
        public const string BaseFeeKey = "Dispatch.Standard.BaseFee";
        public const string SameCityFeeKey = "Dispatch.Standard.SameCityFee";
        public const string DifferentCityFeeKey = "Dispatch.Standard.DifferentCityFee";
        public const string DifferentProvinceFeeKey = "Dispatch.Standard.DifferentProvinceFee";
        public const string SmallFeeKey = "Dispatch.Size.SmallFee";
        public const string MediumFeeKey = "Dispatch.Size.MediumFee";
        public const string LargeFeeKey = "Dispatch.Size.LargeFee";
        public const string ExtraLargeFeeKey = "Dispatch.Size.ExtraLargeFee";
        public const string RiskBufferKey = "Dispatch.RiskBuffer";
        public const string MinimumFeeKey = "Dispatch.MinimumFee";
        public const string MaximumNormalFeeKey = "Dispatch.MaximumNormalFee";
        public const string QuoteExpiryMinutesKey = "Dispatch.QuoteExpiryMinutes";
        public const string CollectionEnabledKey = "Dispatch.CollectionEnabled";

        /// <summary>
        /// (Key, default value as string, description) tuples — the seed set and
        /// the source of truth for fallbacks. Values are invariant-culture.
        /// </summary>
        public static IReadOnlyList<(string Key, string Value, string Description)> Seed { get; } =
            new List<(string, string, string)>
            {
                (DefaultProviderKey, "CourierGuy", "Provider used to quote delivery (CourierGuy / InternalEstimate / Shiplogic). Falls back to InternalEstimate when the courier can't quote."),
                (FallbackToInternalEstimateKey, "true", "Fall back to the deterministic InternalEstimate when the default provider can't quote."),
                (AllowManualFallbackKey, "true", "Legacy alias of FallbackToInternalEstimate."),
                (BaseFeeKey, "80", "Standard delivery base fee (ZAR)."),
                (SameCityFeeKey, "30", "Added when buyer and seller are in the same city."),
                (DifferentCityFeeKey, "60", "Added when buyer and seller are in different cities, same province."),
                (DifferentProvinceFeeKey, "100", "Added when buyer and seller are in different provinces."),
                (SmallFeeKey, "0", "Size surcharge — Small parcel."),
                (MediumFeeKey, "30", "Size surcharge — Medium parcel."),
                (LargeFeeKey, "70", "Size surcharge — Large parcel."),
                (ExtraLargeFeeKey, "120", "Size surcharge — Extra-large parcel."),
                (RiskBufferKey, "20", "Flat risk buffer added to every internal estimate (ZAR)."),
                (MinimumFeeKey, "80", "Minimum delivery fee — quotes are clamped up to this."),
                (MaximumNormalFeeKey, "350", "Maximum normal delivery fee — quotes are clamped down to this."),
                (QuoteExpiryMinutesKey, "5", "How long a presented quote stays valid (minutes). Courier Guy rates expire after ~5 min."),
                (CollectionEnabledKey, "true", "Offer the free 'Arrange Collection' option."),
            };

        /// <summary>
        /// Resolve a typed settings snapshot from active DB setting rows, falling
        /// back to the code defaults above for any missing/unparseable key.
        /// </summary>
        public static ZansiDispatchSettings Resolve(IReadOnlyDictionary<string, string> settings)
        {
            // FallbackToInternalEstimate is the new knob; honour the legacy
            // AllowManualFallback value as the default when it's absent.
            var legacyFallback = GetBool(settings, AllowManualFallbackKey, true);
            var s = new ZansiDispatchSettings
            {
                DefaultProvider = ParseProvider(Get(settings, DefaultProviderKey), ZansiDispatchProviderType.CourierGuy),
                FallbackToInternalEstimate = GetBool(settings, FallbackToInternalEstimateKey, legacyFallback),
                AllowManualFallback = legacyFallback,
                BaseFee = GetDecimal(settings, BaseFeeKey, 80m),
                SameCityFee = GetDecimal(settings, SameCityFeeKey, 30m),
                DifferentCityFee = GetDecimal(settings, DifferentCityFeeKey, 60m),
                DifferentProvinceFee = GetDecimal(settings, DifferentProvinceFeeKey, 100m),
                SmallFee = GetDecimal(settings, SmallFeeKey, 0m),
                MediumFee = GetDecimal(settings, MediumFeeKey, 30m),
                LargeFee = GetDecimal(settings, LargeFeeKey, 70m),
                ExtraLargeFee = GetDecimal(settings, ExtraLargeFeeKey, 120m),
                RiskBuffer = GetDecimal(settings, RiskBufferKey, 20m),
                MinimumFee = GetDecimal(settings, MinimumFeeKey, 80m),
                MaximumNormalFee = GetDecimal(settings, MaximumNormalFeeKey, 350m),
                QuoteExpiryMinutes = (int)GetDecimal(settings, QuoteExpiryMinutesKey, 5m),
                CollectionEnabled = GetBool(settings, CollectionEnabledKey, true),
            };
            return s;
        }

        private static string? Get(IReadOnlyDictionary<string, string> s, string key)
            => s.TryGetValue(key, out var v) ? v : null;

        private static decimal GetDecimal(IReadOnlyDictionary<string, string> s, string key, decimal fallback)
            => s.TryGetValue(key, out var raw)
               && decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? v : fallback;

        private static bool GetBool(IReadOnlyDictionary<string, string> s, string key, bool fallback)
            => s.TryGetValue(key, out var raw) ? raw.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || raw.Trim() == "1" : fallback;

        private static ZansiDispatchProviderType ParseProvider(string? raw, ZansiDispatchProviderType fallback)
            => Enum.TryParse<ZansiDispatchProviderType>(raw, ignoreCase: true, out var v) && Enum.IsDefined(typeof(ZansiDispatchProviderType), v)
                ? v : fallback;
    }
}
