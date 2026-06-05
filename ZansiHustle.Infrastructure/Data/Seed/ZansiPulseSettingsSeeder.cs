using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.ZansiPulse;
using ZansiHustle.Domain.ZansiPulse;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds the default ZansiPulse tuning knobs (event weights, recommendation
    /// blend weights, interest bounds) into <c>ZansiPulseSettings</c> on
    /// startup. Idempotent: only inserts keys that are missing, so operator
    /// edits to existing rows are never overwritten. Values mirror
    /// <see cref="ZansiPulseDefaults"/> — the same numbers the service falls
    /// back to when a row is absent — so seeding is purely about surfacing the
    /// knobs for tuning, never about changing behaviour.
    /// </summary>
    public static class ZansiPulseSettingsSeeder
    {
        public static async Task SeedAsync(AppDbContext db)
        {
            var existingKeys = await db.ZansiPulseSettings
                .Select(s => s.Key)
                .ToListAsync();
            var present = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var defaults = BuildDefaults();
            var now = DateTime.UtcNow;
            var added = false;

            foreach (var (key, value, description) in defaults)
            {
                if (present.Contains(key)) continue;
                db.ZansiPulseSettings.Add(new ZansiPulseSetting
                {
                    Id = Guid.NewGuid(),
                    Key = key,
                    Value = value,
                    Description = description,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
                added = true;
            }

            if (added)
                await db.SaveChangesAsync();
        }

        private static List<(string Key, string Value, string Description)> BuildDefaults()
        {
            var list = new List<(string, string, string)>();

            foreach (var kvp in ZansiPulseDefaults.EventWeights)
            {
                list.Add((
                    ZansiPulseDefaults.EventWeightKey(kvp.Key),
                    Str(kvp.Value),
                    $"Behavioural weight applied when a '{kvp.Key}' event is tracked."));
            }

            list.Add((ZansiPulseDefaults.RecWeightUserInterestKey, Str(ZansiPulseDefaults.RecUserInterestMatch), "Recommendation blend weight — user interest match (fraction)."));
            list.Add((ZansiPulseDefaults.RecWeightLocationKey, Str(ZansiPulseDefaults.RecLocationMatch), "Recommendation blend weight — location match (fraction)."));
            list.Add((ZansiPulseDefaults.RecWeightEngagementKey, Str(ZansiPulseDefaults.RecListingEngagement), "Recommendation blend weight — listing engagement (fraction)."));
            list.Add((ZansiPulseDefaults.RecWeightSellerQualityKey, Str(ZansiPulseDefaults.RecSellerQuality), "Recommendation blend weight — seller quality (fraction)."));
            list.Add((ZansiPulseDefaults.RecWeightRecencyKey, Str(ZansiPulseDefaults.RecRecency), "Recommendation blend weight — recency (fraction)."));
            list.Add((ZansiPulseDefaults.RecWeightPlatformBoostKey, Str(ZansiPulseDefaults.RecPlatformBoost), "Recommendation blend weight — platform boost (fraction)."));

            list.Add((ZansiPulseDefaults.OnboardingScoreKey, Str(ZansiPulseDefaults.OnboardingInterestScore), "Starting interest score applied per onboarding-selected category."));
            list.Add((ZansiPulseDefaults.MinScoreKey, Str(ZansiPulseDefaults.MinInterestScore), "Lower clamp for a user interest score."));
            list.Add((ZansiPulseDefaults.MaxScoreKey, Str(ZansiPulseDefaults.MaxInterestScore), "Upper clamp for a user interest score."));
            list.Add((ZansiPulseDefaults.NormalizerKey, Str(ZansiPulseDefaults.InterestNormalizer), "Denominator that normalises a raw interest score to the 0-1 recommendation factor."));

            return list;
        }

        private static string Str(decimal d) => d.ToString(CultureInfo.InvariantCulture);
    }
}
