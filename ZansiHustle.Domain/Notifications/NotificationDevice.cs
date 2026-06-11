using System;
using ZansiHustle.Shared.Enums.Notifications;

namespace ZansiHustle.Domain.Notifications
{
    /// <summary>
    /// A registered push target (one per device/app install) for a user. The
    /// push service looks these up to deliver OneSignal notifications. Devices
    /// are upserted by (UserId, Provider, PlayerId) and flipped inactive on
    /// logout — never hard-deleted, so delivery history is auditable.
    /// </summary>
    public class NotificationDevice
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public PushProvider Provider { get; set; } = PushProvider.OneSignal;

        /// <summary>OneSignal subscription / player id for this install.</summary>
        public string PlayerId { get; set; } = string.Empty;

        public DevicePlatform Platform { get; set; } = DevicePlatform.Unknown;

        public string? AppVersion { get; set; }
        public string? DeviceName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAtUtc { get; set; } = DateTime.UtcNow;
    }
}
