using System;
using ZansiHustle.Shared.Enums.AppVersion;

namespace ZansiHustle.Domain.AppVersion
{
    /// <summary>
    /// A per-(Platform, Channel) mobile update/version-control rule. One row per
    /// store target (e.g. Android/Google, Android/Huawei, iOS/Apple) — enforced by
    /// a UNIQUE index on (Platform, Channel). Read by the public version-check
    /// endpoint and the App-Version gate middleware to decide whether an installed
    /// build is forced-outdated (hard update) or merely has a softer update
    /// available. A missing/disabled row falls back to a Platform+Generic rule and
    /// then to the appsettings "MobileAppVersion" section — so force-update is NEVER
    /// implied by absence. UTC timestamps; <see cref="RowVersion"/> is the
    /// optimistic-concurrency token for admin edits.
    /// </summary>
    public class MobileAppVersionRule
    {
        public Guid Id { get; set; }

        public MobileAppPlatform Platform { get; set; }
        public MobileAppChannel Channel { get; set; }

        /// <summary>Latest published version (semver "x.y.z").</summary>
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>Latest published build number (monotonic integer).</summary>
        public int LatestBuildNumber { get; set; }

        /// <summary>Lowest version still allowed to run (semver "x.y.z").</summary>
        public string MinimumSupportedVersion { get; set; } = string.Empty;

        /// <summary>Lowest build number still allowed to run.</summary>
        public int MinimumSupportedBuildNumber { get; set; }

        /// <summary>Operator hard-override: force a blocking update regardless of version math.</summary>
        public bool UpdateRequired { get; set; }

        /// <summary>Operator soft-override: advertise an optional update.</summary>
        public bool UpdateAvailable { get; set; }

        /// <summary>When false the row is ignored (falls back to Generic / appsettings).</summary>
        public bool IsEnabled { get; set; } = true;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string PrimaryButtonText { get; set; } = string.Empty;
        public string SecondaryButtonText { get; set; } = string.Empty;
        public string StoreUrl { get; set; } = string.Empty;

        /// <summary>Optional human-readable changelog for the latest version.</summary>
        public string? ReleaseNotes { get; set; }

        /// <summary>Admin who last upserted/toggled the rule (audit only; no FK).</summary>
        public Guid? UpdatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>Optimistic-concurrency token (rowversion).</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
