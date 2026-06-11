namespace ZansiHustle.Application.Notifications.Dtos
{
    /// <summary>Payload to register/refresh a push device for the current user.</summary>
    public class RegisterDeviceRequestDto
    {
        /// <summary>OneSignal subscription / player id.</summary>
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>"ios" | "android" | "web" (case-insensitive). Optional.</summary>
        public string? Platform { get; set; }

        public string? AppVersion { get; set; }
        public string? DeviceName { get; set; }
    }

    /// <summary>Payload to deactivate a push device (logout).</summary>
    public class UnregisterDeviceRequestDto
    {
        public string PlayerId { get; set; } = string.Empty;
    }
}
