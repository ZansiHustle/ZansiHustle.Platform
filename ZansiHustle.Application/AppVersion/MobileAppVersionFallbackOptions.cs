namespace ZansiHustle.Application.AppVersion
{
    /// <summary>
    /// Code/config fallback for the mobile version check, bound from the
    /// appsettings <c>"MobileAppVersion"</c> section. Used ONLY when no DB rule
    /// (exact or Platform+Generic) matches the request — so the public endpoint
    /// and the gate middleware always have a safe answer even before any
    /// <c>MobileAppVersionRules</c> row exists. Ships force-update OFF by default.
    /// </summary>
    public sealed class MobileAppVersionFallbackOptions
    {
        public const string SectionName = "MobileAppVersion";

        public string LatestVersion { get; set; } = "1.0.0";
        public int LatestBuildNumber { get; set; } = 1;
        public string MinimumSupportedVersion { get; set; } = "1.0.0";
        public int MinimumSupportedBuildNumber { get; set; } = 1;

        /// <summary>Hard force-update flag. MUST default to false (fail-safe).</summary>
        public bool UpdateRequired { get; set; } = false;

        /// <summary>Soft "update available" flag.</summary>
        public bool UpdateAvailable { get; set; } = false;

        public string Title { get; set; } = "Update available";
        public string Message { get; set; } = "A new version of the app is available.";
        public string PrimaryButtonText { get; set; } = "Update now";
        public string SecondaryButtonText { get; set; } = "Later";
        public string StoreUrl { get; set; } = "https://play.google.com/store/apps/details?id=com.zansihustle.app";
        public string? ReleaseNotes { get; set; }
    }
}
