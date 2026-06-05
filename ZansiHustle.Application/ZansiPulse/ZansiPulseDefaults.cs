using System.Collections.Generic;
using ZansiHustle.Shared.Enums.ZansiPulse;

namespace ZansiHustle.Application.ZansiPulse
{
    /// <summary>
    /// Code-default values for every tunable ZansiPulse knob. These are the
    /// fallback used whenever a matching <c>ZansiPulseSetting</c> row is
    /// absent or inactive, so the intelligence layer always has sane numbers
    /// even before (or instead of) database tuning. The seeder writes these
    /// same values into <c>ZansiPulseSettings</c> on first run; operators can
    /// then edit the rows to retune without a deploy.
    ///
    /// Keys are namespaced strings so all ZansiPulse settings share one table
    /// without collision: <c>EventWeight:{EventType}</c>,
    /// <c>RecWeight:{Factor}</c>, <c>Interest:{Knob}</c>.
    /// </summary>
    public static class ZansiPulseDefaults
    {
        // ── Behavioural event weights ───────────────────────────────────────
        public static IReadOnlyDictionary<ZansiPulseEventType, decimal> EventWeights { get; } =
            new Dictionary<ZansiPulseEventType, decimal>
            {
                [ZansiPulseEventType.ViewListing] = 1m,
                [ZansiPulseEventType.OpenListingDetail] = 3m,
                [ZansiPulseEventType.SearchCategory] = 4m,
                [ZansiPulseEventType.SearchTerm] = 4m,
                [ZansiPulseEventType.OpenSellerProfile] = 4m,
                [ZansiPulseEventType.OpenShopProfile] = 4m,
                [ZansiPulseEventType.FavouriteListing] = 8m,
                [ZansiPulseEventType.ShareListing] = 8m,
                [ZansiPulseEventType.MessageSeller] = 12m,
                [ZansiPulseEventType.OrderIntent] = 20m,
                [ZansiPulseEventType.HideListing] = -8m,
                [ZansiPulseEventType.NotInterested] = -12m,
                [ZansiPulseEventType.ReportListing] = -30m,
            };

        // ── Recommendation blend weights (fractions, sum ≈ 1.0) ─────────────
        public const decimal RecUserInterestMatch = 0.30m;
        public const decimal RecLocationMatch = 0.20m;
        public const decimal RecListingEngagement = 0.20m;
        public const decimal RecSellerQuality = 0.15m;
        public const decimal RecRecency = 0.10m;
        public const decimal RecPlatformBoost = 0.05m;

        // ── User-interest scoring bounds + onboarding seed ──────────────────
        /// <summary>Strong starting score applied to each onboarding-selected category.</summary>
        public const decimal OnboardingInterestScore = 50m;

        /// <summary>Lower clamp — a disliked category bottoms out here, never negative-infinity.</summary>
        public const decimal MinInterestScore = 0m;

        /// <summary>Upper clamp — keeps a heavily-engaged category from running away.</summary>
        public const decimal MaxInterestScore = 1000m;

        /// <summary>
        /// Denominator used to normalise a raw interest score into the 0–1
        /// factor the recommender blends. A user at/above this score on a
        /// category is treated as a full-strength interest match.
        /// </summary>
        public const decimal InterestNormalizer = 100m;

        // ── Setting keys ────────────────────────────────────────────────────
        public static string EventWeightKey(ZansiPulseEventType type) => $"EventWeight:{type}";

        public const string RecWeightUserInterestKey = "RecWeight:UserInterestMatch";
        public const string RecWeightLocationKey = "RecWeight:LocationMatch";
        public const string RecWeightEngagementKey = "RecWeight:ListingEngagement";
        public const string RecWeightSellerQualityKey = "RecWeight:SellerQuality";
        public const string RecWeightRecencyKey = "RecWeight:Recency";
        public const string RecWeightPlatformBoostKey = "RecWeight:PlatformBoost";

        public const string OnboardingScoreKey = "Interest:OnboardingScore";
        public const string MinScoreKey = "Interest:MinScore";
        public const string MaxScoreKey = "Interest:MaxScore";
        public const string NormalizerKey = "Interest:Normalizer";
    }
}
