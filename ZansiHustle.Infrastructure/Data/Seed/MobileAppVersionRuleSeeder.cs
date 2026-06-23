using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Domain.AppVersion;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.AppVersion;

namespace ZansiHustle.Infrastructure.Data.Seed
{
    /// <summary>
    /// Seeds the three baseline mobile version rules (Android/Google,
    /// Android/Huawei, iOS/Apple) on startup. Idempotent: only inserts a
    /// (Platform, Channel) row that is missing, so operator edits made via the
    /// admin endpoints are never overwritten.
    ///
    /// LAUNCH-SAFE: every seeded row ships UpdateRequired=false and
    /// UpdateAvailable=false at v1.0.0 / build 1 — the gate can NEVER force an
    /// update purely from seed data. Only the Google row carries a real Play
    /// Store URL (package com.zansihustle.app); Huawei + Apple are left blank
    /// until their store listings exist.
    /// </summary>
    public static class MobileAppVersionRuleSeeder
    {
        private const string GooglePlayUrl =
            "https://play.google.com/store/apps/details?id=com.zansihustle.app";

        public static async Task SeedAsync(AppDbContext db)
        {
            var existing = await db.MobileAppVersionRules
                .Select(x => new { x.Platform, x.Channel })
                .ToListAsync();

            var present = existing
                .Select(x => (x.Platform, x.Channel))
                .ToHashSet();

            var now = DateTime.UtcNow;
            var added = false;

            void EnsureRule(MobileAppPlatform platform, MobileAppChannel channel, string storeUrl)
            {
                if (present.Contains((platform, channel)))
                    return;

                db.MobileAppVersionRules.Add(new MobileAppVersionRule
                {
                    Id = Guid.NewGuid(),
                    Platform = platform,
                    Channel = channel,
                    LatestVersion = "1.0.0",
                    LatestBuildNumber = 1,
                    MinimumSupportedVersion = "1.0.0",
                    MinimumSupportedBuildNumber = 1,
                    UpdateRequired = false,
                    UpdateAvailable = false,
                    IsEnabled = true,
                    Title = "Update available",
                    Message = "A new version of ZansiHustle is available.",
                    PrimaryButtonText = "Update now",
                    SecondaryButtonText = "Later",
                    StoreUrl = storeUrl,
                    ReleaseNotes = null,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                });
                added = true;
            }

            EnsureRule(MobileAppPlatform.Android, MobileAppChannel.Google, GooglePlayUrl);
            EnsureRule(MobileAppPlatform.Android, MobileAppChannel.Huawei, string.Empty);
            EnsureRule(MobileAppPlatform.iOS, MobileAppChannel.Apple, string.Empty);

            if (added)
                await db.SaveChangesAsync();
        }
    }
}
