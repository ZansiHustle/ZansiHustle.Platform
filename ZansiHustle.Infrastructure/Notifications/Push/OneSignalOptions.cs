namespace ZansiHustle.Infrastructure.Notifications.Push
{
    /// <summary>Bound from the "OneSignal" config section. When
    /// <see cref="Enabled"/> is false or the keys are blank, push is skipped
    /// (the Null implementation is used / the OneSignal impl no-ops).</summary>
    public class OneSignalOptions
    {
        public const string SectionName = "OneSignal";

        public bool Enabled { get; set; }
        public string? AppId { get; set; }
        public string? RestApiKey { get; set; }

        public bool IsConfigured =>
            Enabled
            && !string.IsNullOrWhiteSpace(AppId)
            && !string.IsNullOrWhiteSpace(RestApiKey);
    }
}
