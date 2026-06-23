using ZansiHustle.Shared.Enums.AppVersion;

namespace ZansiHustle.Application.AppVersion.Dtos
{
    /// <summary>
    /// Admin create/update payload for a mobile version rule. Validated by the
    /// service: semver shape on both versions, build numbers &gt;= 0, no duplicate
    /// (Platform, Channel), and a non-empty <see cref="StoreUrl"/> whenever
    /// <see cref="UpdateRequired"/> is true (you cannot force a blocking update
    /// without telling the user where to go).
    /// </summary>
    public sealed class UpsertMobileAppVersionRuleRequestDto
    {
        public MobileAppPlatform Platform { get; set; }
        public MobileAppChannel Channel { get; set; }

        public string LatestVersion { get; set; } = string.Empty;
        public int LatestBuildNumber { get; set; }
        public string MinimumSupportedVersion { get; set; } = string.Empty;
        public int MinimumSupportedBuildNumber { get; set; }

        public bool UpdateRequired { get; set; }
        public bool UpdateAvailable { get; set; }
        public bool IsEnabled { get; set; } = true;

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string PrimaryButtonText { get; set; } = string.Empty;
        public string SecondaryButtonText { get; set; } = string.Empty;
        public string StoreUrl { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }
    }
}
