namespace ZansiHustle.Application.AppVersion.Dtos
{
    /// <summary>
    /// Public version-check answer returned by GET /api/app-version/mobile. All
    /// fields are camelCased on the wire. This is the single contract shared by
    /// the portal and the mobile app's update gate. force-update is implied ONLY
    /// by <see cref="UpdateRequired"/> — never by absence of data.
    /// </summary>
    public sealed class MobileAppVersionCheckResponseDto
    {
        /// <summary>Hard, blocking update: the installed build may not continue.</summary>
        public bool UpdateRequired { get; set; }

        /// <summary>Soft, optional update advertised to the user.</summary>
        public bool UpdateAvailable { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string PrimaryButtonText { get; set; } = string.Empty;
        public string SecondaryButtonText { get; set; } = string.Empty;
        public string StoreUrl { get; set; } = string.Empty;
        public string? ReleaseNotes { get; set; }

        public string LatestVersion { get; set; } = string.Empty;
        public int LatestBuildNumber { get; set; }
        public string MinimumSupportedVersion { get; set; } = string.Empty;
        public int MinimumSupportedBuildNumber { get; set; }
    }
}
