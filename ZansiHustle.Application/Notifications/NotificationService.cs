using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Notifications.Dtos;
using ZansiHustle.Application.Persistence.Notifications;
using ZansiHustle.Application.Realtime;
using ZansiHustle.Domain.Notifications;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Shared.Enums.Notifications;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Notifications
{
    /// <inheritdoc />
    public sealed class NotificationService : INotificationService
    {
        private const int MaxTake = 100;

        private readonly INotificationRepository _notifications;
        private readonly INotificationDeviceRepository _devices;
        private readonly IRealtimeNotifier _realtime;
        private readonly IPushNotificationService _push;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository notifications,
            INotificationDeviceRepository devices,
            IRealtimeNotifier realtime,
            IPushNotificationService push,
            ILogger<NotificationService> logger)
        {
            _notifications = notifications;
            _devices = devices;
            _realtime = realtime;
            _push = push;
            _logger = logger;
        }

        // ─── Read ──────────────────────────────────────────────────────────────

        public async Task<Result<NotificationListDto>> GetForUserAsync(Guid userId, int take = 50)
        {
            if (userId == Guid.Empty)
                return Result<NotificationListDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            take = Math.Clamp(take <= 0 ? 50 : take, 1, MaxTake);
            var items = await _notifications.GetForUserAsync(userId, take);
            var unread = await _notifications.GetUnreadCountAsync(userId);

            return Result<NotificationListDto>.Success(new NotificationListDto
            {
                Items = items.Select(ToDto).ToList(),
                UnreadCount = unread
            }, "Notifications loaded.");
        }

        public async Task<Result<UnreadCountDto>> GetUnreadCountAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return Result<UnreadCountDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            var count = await _notifications.GetUnreadCountAsync(userId);
            return Result<UnreadCountDto>.Success(new UnreadCountDto { UnreadCount = count }, "Unread count loaded.");
        }

        public async Task<Result> MarkReadAsync(Guid userId, Guid notificationId)
        {
            if (userId == Guid.Empty)
                return Result.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            var notification = await _notifications.GetByIdAsync(notificationId);
            if (notification is null || notification.UserId != userId)
                return Result.Failure(ErrorCodes.NotFound, "Notification not found.");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAtUtc = DateTime.UtcNow;
                _notifications.Update(notification);
                await _notifications.SaveChangesAsync();
                await SafeUnreadCountAsync(userId);
            }

            return Result.Success("Notification marked read.");
        }

        public async Task<Result> MarkAllReadAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return Result.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            var changed = await _notifications.MarkAllReadAsync(userId, DateTime.UtcNow);
            if (changed > 0) await SafeUnreadCountAsync(userId);
            return Result.Success("Notifications marked read.");
        }

        // ─── Device registration ────────────────────────────────────────────────

        public async Task<Result> RegisterDeviceAsync(Guid userId, RegisterDeviceRequestDto request)
        {
            if (userId == Guid.Empty)
                return Result.Failure(ErrorCodes.Unauthorized, "User identifier not found.");
            if (request is null || string.IsNullOrWhiteSpace(request.PlayerId))
                return Result.Failure(ErrorCodes.BadRequest, "A device player id is required.");

            var playerId = request.PlayerId.Trim();
            var platform = ParsePlatform(request.Platform);
            var nowUtc = DateTime.UtcNow;

            var existing = await _devices.GetByPlayerIdAsync(PushProvider.OneSignal, playerId);
            if (existing is null)
            {
                await _devices.AddAsync(new NotificationDevice
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Provider = PushProvider.OneSignal,
                    PlayerId = playerId,
                    Platform = platform,
                    AppVersion = Trim(request.AppVersion),
                    DeviceName = Trim(request.DeviceName),
                    IsActive = true,
                    CreatedAtUtc = nowUtc,
                    LastSeenAtUtc = nowUtc
                });
            }
            else
            {
                // Re-home the player id to the current user (device handed over /
                // re-login) and refresh its metadata.
                existing.UserId = userId;
                existing.Platform = platform;
                existing.AppVersion = Trim(request.AppVersion) ?? existing.AppVersion;
                existing.DeviceName = Trim(request.DeviceName) ?? existing.DeviceName;
                existing.IsActive = true;
                existing.LastSeenAtUtc = nowUtc;
                _devices.Update(existing);
            }

            await _devices.SaveChangesAsync();
            return Result.Success("Device registered.");
        }

        public async Task<Result> UnregisterDeviceAsync(Guid userId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return Result.Failure(ErrorCodes.BadRequest, "A device player id is required.");

            await _devices.DeactivateAsync(PushProvider.OneSignal, playerId.Trim());
            await _devices.SaveChangesAsync();
            return Result.Success("Device unregistered.");
        }

        // ─── Create + deliver ────────────────────────────────────────────────────

        public async Task<Notification> CreateAndDispatchAsync(
            Guid userId, NotificationType type, string title, string body, object? data = null)
        {
            var dataDict = ToStringDict(data);
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                DataJson = dataDict.Count > 0 ? JsonSerializer.Serialize(dataDict) : null,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _notifications.AddAsync(notification);
            await _notifications.SaveChangesAsync();

            // Best-effort delivery — never let transport failures bubble.
            await SafeDeliverAsync(notification, dataDict);

            return notification;
        }

        // ─── Workflow helpers ────────────────────────────────────────────────────

        public async Task NotifySellerBookingRequestedAsync(ServiceBooking booking)
        {
            var sellerUserId = booking.Merchant?.OwnerUserId;
            if (sellerUserId is null || sellerUserId == Guid.Empty)
            {
                _logger.LogWarning(
                    "[Notifications] Cannot notify seller for booking {BookingId} — merchant owner not resolved.",
                    booking.Id);
                return;
            }

            var serviceName = string.IsNullOrWhiteSpace(booking.Listing?.Title)
                ? "your service"
                : booking.Listing!.Title;

            try
            {
                await CreateAndDispatchAsync(
                    sellerUserId.Value,
                    NotificationType.SellerBookingRequested,
                    "New booking request",
                    $"You have a new booking for {serviceName}.",
                    BookingData(booking, "SellerBooking"));

                await _realtime.BookingStatusChangedAsync(
                    sellerUserId.Value, BookingStatusPayload(booking));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Notifications] Failed to notify seller for booking {BookingId}.", booking.Id);
            }
        }

        public async Task NotifyBookingStatusChangedAsync(
            ServiceBooking booking, Guid recipientUserId, NotificationType type, string title, string body)
        {
            if (recipientUserId == Guid.Empty) return;
            try
            {
                var targetType = recipientUserId == booking.Merchant?.OwnerUserId
                    ? "SellerBooking"
                    : "Order";
                await CreateAndDispatchAsync(recipientUserId, type, title, body, BookingData(booking, targetType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Notifications] Failed to notify booking status change for {BookingId}.", booking.Id);
            }
        }

        // ─── Product order acceptance lifecycle ──────────────────────────────────

        public async Task NotifySellerProductOrderRequestedAsync(Order order)
        {
            var sellerUserId = order.Merchant?.OwnerUserId;
            if (sellerUserId is null || sellerUserId == Guid.Empty)
            {
                _logger.LogWarning(
                    "[Notifications] Cannot notify seller for order {OrderId} — merchant owner not resolved.",
                    order.Id);
                return;
            }
            try
            {
                await CreateAndDispatchAsync(
                    sellerUserId.Value,
                    NotificationType.SellerOrderRequested,
                    "New order to confirm",
                    $"You have a new paid order ({order.Code}). Accept it to start fulfilment.",
                    OrderData(order, "SellerOrder"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notifications] Failed to notify seller for order {OrderId}.", order.Id);
            }
        }

        public async Task NotifyCustomerOrderAwaitingAcceptanceAsync(Order order)
        {
            try
            {
                await CreateAndDispatchAsync(
                    order.BuyerUserId,
                    NotificationType.OrderAwaitingSellerAcceptance,
                    "Payment received",
                    $"Your payment for {order.Code} is secured. We're waiting for the seller to confirm this order.",
                    OrderData(order, "Order"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notifications] Failed to notify buyer (awaiting) for order {OrderId}.", order.Id);
            }
        }

        public async Task NotifyCustomerOrderAcceptedAsync(Order order)
        {
            try
            {
                await CreateAndDispatchAsync(
                    order.BuyerUserId,
                    NotificationType.OrderAcceptedBySeller,
                    "Order accepted",
                    $"The seller accepted your order {order.Code}. Dispatch updates will appear once arranged.",
                    OrderData(order, "Order"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notifications] Failed to notify buyer (accepted) for order {OrderId}.", order.Id);
            }
        }

        public async Task NotifyCustomerOrderRejectedAsync(Order order, string? reason)
        {
            try
            {
                var tail = string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}";
                await CreateAndDispatchAsync(
                    order.BuyerUserId,
                    NotificationType.OrderRejectedBySeller,
                    "Order could not be fulfilled",
                    $"The seller could not fulfil order {order.Code}. Your payment was refunded to your wallet.{tail}",
                    OrderData(order, "Order"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notifications] Failed to notify buyer (rejected) for order {OrderId}.", order.Id);
            }
        }

        private static object OrderData(Order o, string targetType) => new
        {
            targetType,
            orderId = o.Id.ToString(),
            code = o.Code
        };

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private async Task SafeDeliverAsync(Notification notification, Dictionary<string, string> dataDict)
        {
            try
            {
                var payload = new
                {
                    notificationId = notification.Id,
                    type = notification.Type.ToString(),
                    title = notification.Title,
                    body = notification.Body,
                    data = dataDict,
                    createdAtUtc = notification.CreatedAtUtc
                };
                await _realtime.NotificationCreatedAsync(notification.UserId, payload);
                await SafeUnreadCountAsync(notification.UserId);
                await _push.SendToUserAsync(notification.UserId, notification.Title, notification.Body, dataDict);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Notifications] Best-effort delivery failed for {NotificationId}.", notification.Id);
            }
        }

        private async Task SafeUnreadCountAsync(Guid userId)
        {
            try
            {
                var count = await _notifications.GetUnreadCountAsync(userId);
                await _realtime.UnreadCountChangedAsync(userId, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notifications] Unread-count push failed for user {UserId}.", userId);
            }
        }

        private static object BookingData(ServiceBooking b, string targetType) => new
        {
            targetType,
            bookingId = b.Id.ToString(),
            orderId = b.OrderId.ToString(),
            listingId = b.ListingId.ToString()
        };

        private static object BookingStatusPayload(ServiceBooking b) => new
        {
            bookingId = b.Id.ToString(),
            status = NormaliseStatus(b.Status),
            orderId = b.OrderId.ToString(),
            listingId = b.ListingId.ToString(),
            changedAtUtc = (b.UpdatedAtUtc ?? b.CreatedAtUtc)
        };

        private static string NormaliseStatus(ServiceBookingStatus status) =>
            status == ServiceBookingStatus.Confirmed
                ? ServiceBookingStatus.Requested.ToString()
                : status.ToString();

        private static NotificationDto ToDto(Notification n)
        {
            var data = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(n.DataJson))
            {
                try
                {
                    data = JsonSerializer.Deserialize<Dictionary<string, string>>(n.DataJson)
                           ?? new Dictionary<string, string>();
                }
                catch { /* tolerate malformed legacy payloads */ }
            }

            return new NotificationDto
            {
                Id = n.Id,
                Type = n.Type.ToString(),
                Title = n.Title,
                Body = n.Body,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc,
                Data = data
            };
        }

        private static Dictionary<string, string> ToStringDict(object? data)
        {
            var dict = new Dictionary<string, string>();
            if (data is null) return dict;
            try
            {
                using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var value = prop.Value.ValueKind switch
                    {
                        JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                        JsonValueKind.Null => string.Empty,
                        _ => prop.Value.ToString()
                    };
                    dict[prop.Name] = value;
                }
            }
            catch { /* non-object payloads are simply ignored */ }
            return dict;
        }

        private static DevicePlatform ParsePlatform(string? platform) =>
            (platform ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "ios" => DevicePlatform.Ios,
                "android" => DevicePlatform.Android,
                "web" => DevicePlatform.Web,
                _ => DevicePlatform.Unknown
            };

        private static string? Trim(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
