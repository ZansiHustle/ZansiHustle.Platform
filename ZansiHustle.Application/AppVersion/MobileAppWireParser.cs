using ZansiHustle.Shared.Enums.AppVersion;

namespace ZansiHustle.Application.AppVersion
{
    /// <summary>
    /// Lenient wire parsing for the mobile version contract. Accepts lowercase
    /// strings (also tolerant of casing / numeric values), and defaults to the
    /// safe baseline (Android / Google) when the value is missing or unknown — so
    /// a malformed client header never breaks the check.
    /// </summary>
    public static class MobileAppWireParser
    {
        public static MobileAppPlatform ParsePlatform(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return MobileAppPlatform.Android;

            return value.Trim().ToLowerInvariant() switch
            {
                "android" or "0" => MobileAppPlatform.Android,
                "ios" or "1" => MobileAppPlatform.iOS,
                _ => MobileAppPlatform.Android
            };
        }

        public static MobileAppChannel ParseChannel(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return MobileAppChannel.Google;

            return value.Trim().ToLowerInvariant() switch
            {
                "google" or "0" => MobileAppChannel.Google,
                "huawei" or "1" => MobileAppChannel.Huawei,
                "apple" or "2" => MobileAppChannel.Apple,
                "generic" or "3" => MobileAppChannel.Generic,
                _ => MobileAppChannel.Google
            };
        }

        /// <summary>Parses a build-number string; returns null when absent/invalid.</summary>
        public static int? ParseBuildNumber(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return int.TryParse(value.Trim(), out var n) ? n : (int?)null;
        }
    }
}
