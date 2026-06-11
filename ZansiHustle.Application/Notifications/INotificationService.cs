using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Notifications.Dtos;
using ZansiHustle.Domain.Notifications;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Shared.Enums.Notifications;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Notifications
{
    /// <summary>
    /// Persists notifications (source of truth) and fans them out best-effort
    /// over SignalR (in-app) + OneSignal (push). Read endpoints + the booking
    /// workflow both depend on this.
    /// </summary>
    public interface INotificationService
    {
        // ── Read ──────────────────────────────────────────────────────────────
        Task<Result<NotificationListDto>> GetForUserAsync(Guid userId, int take = 50);
        Task<Result<UnreadCountDto>> GetUnreadCountAsync(Guid userId);
        Task<Result> MarkReadAsync(Guid userId, Guid notificationId);
        Task<Result> MarkAllReadAsync(Guid userId);

        // ── Device registration ────────────────────────────────────────────────
        Task<Result> RegisterDeviceAsync(Guid userId, RegisterDeviceRequestDto request);
        Task<Result> UnregisterDeviceAsync(Guid userId, string playerId);

        // ── Create + deliver (low-level) ───────────────────────────────────────
        /// <summary>
        /// Persist a notification for a user and best-effort deliver it over
        /// SignalR + push. <paramref name="data"/> is serialised to DataJson and
        /// also sent as the realtime/push payload. Never throws on delivery
        /// failure. Returns the created entity.
        /// </summary>
        Task<Notification> CreateAndDispatchAsync(
            Guid userId,
            NotificationType type,
            string title,
            string body,
            object? data = null);

        // ── Workflow helpers (high-level) ──────────────────────────────────────
        /// <summary>Seller alert when a buyer pays for a booking (→ Requested).
        /// Reads the merchant owner + listing title from the booking's loaded
        /// navigations; no-ops safely if the owner can't be resolved.</summary>
        Task NotifySellerBookingRequestedAsync(ServiceBooking booking);

        /// <summary>Notify the other party that a booking changed status.</summary>
        Task NotifyBookingStatusChangedAsync(
            ServiceBooking booking,
            Guid recipientUserId,
            NotificationType type,
            string title,
            string body);
    }
}
