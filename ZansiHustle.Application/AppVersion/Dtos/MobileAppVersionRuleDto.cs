using System;
using ZansiHustle.Shared.Enums.AppVersion;

namespace ZansiHustle.Application.AppVersion.Dtos
{
    /// <summary>
    /// Admin-facing projection of a <c>MobileAppVersionRule</c> row. Returned by
    /// the admin list/detail endpoints. Carries the <see cref="RowVersion"/> token
    /// (base64) so the portal can round-trip optimistic concurrency on edits.
    /// </summary>
    public sealed class MobileAppVersionRuleDto
    {
        public Guid Id { get; set; }

        public MobileAppPlatform Platform { get; set; }
        public MobileAppChannel Channel { get; set; }

        public string LatestVersion { get; set; } = string.Empty;
        public int LatestBuildNumber { get; set; }
        public string MinimumSupportedVersion { get; set; } = string.Empty;
        public int MinimumSupportedBuildNumber { get; set; }

        public bool UpdateRequired { get; set; }
        public bool UpdateAvailable { get; set; }
        public bool IsEnabled { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string PrimaryButtonText { get; set; } = string.Empty;
        public string SecondaryButtonText { get; set; } = string.Empty;
        public string StoreUrl { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }

        public Guid? UpdatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        /// <summary>Base64 optimistic-concurrency token (echo back on PUT).</summary>
        public string? RowVersion { get; set; }
    }
}
