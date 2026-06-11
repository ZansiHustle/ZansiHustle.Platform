using System;
using ZansiHustle.Shared.Enums.Notifications;

namespace ZansiHustle.Domain.Notifications
{
    /// <summary>
    /// A single in-app notification addressed to one user. The authoritative
    /// record behind the bell icon + notifications page (REST is the source of
    /// truth; SignalR/OneSignal are best-effort delivery on top).
    ///
    /// <see cref="DataJson"/> carries an actionable payload (targetType + ids)
    /// the client uses to deep-link a tap — e.g.
    /// <c>{"targetType":"SellerBooking","bookingId":"…","orderId":"…","listingId":"…"}</c>.
    /// Kept as opaque JSON so new notification kinds don't require schema changes.
    /// </summary>
    public class Notification
    {
        public Guid Id { get; set; }

        /// <summary>Recipient user id.</summary>
        public Guid UserId { get; set; }

        public NotificationType Type { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        /// <summary>Opaque JSON deep-link payload (nullable).</summary>
        public string? DataJson { get; set; }

        public bool IsRead { get; set; }
        public DateTime? ReadAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
