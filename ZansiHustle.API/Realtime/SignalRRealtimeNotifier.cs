using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Realtime;

namespace ZansiHustle.API.Realtime
{
    /// <summary>
    /// SignalR-backed <see cref="IRealtimeNotifier"/>. Pushes to the single
    /// recipient via <c>Clients.User(...)</c> (SignalR matches the JWT
    /// NameIdentifier claim, which is the user's Guid). Best-effort — every send
    /// is wrapped so a transport hiccup never bubbles into the business call;
    /// REST stays the source of truth and clients also refetch.
    /// </summary>
    public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<RealtimeHub> _hub;
        private readonly ILogger<SignalRRealtimeNotifier> _logger;

        public SignalRRealtimeNotifier(
            IHubContext<RealtimeHub> hub,
            ILogger<SignalRRealtimeNotifier> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public Task NotificationCreatedAsync(Guid userId, object payload) =>
            SendAsync(userId, "NotificationCreated", payload);

        public Task UnreadCountChangedAsync(Guid userId, int unreadCount) =>
            SendAsync(userId, "NotificationUnreadCountChanged", new { unreadCount });

        public Task BookingStatusChangedAsync(Guid userId, object payload) =>
            SendAsync(userId, "BookingStatusChanged", payload);

        public Task WalletBalanceChangedAsync(Guid userId, object payload) =>
            SendAsync(userId, "WalletBalanceChanged", payload);

        private async Task SendAsync(Guid userId, string method, object payload)
        {
            if (userId == Guid.Empty) return;
            try
            {
                await _hub.Clients
                    .User(userId.ToString("D", CultureInfo.InvariantCulture))
                    .SendAsync(method, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Realtime] Failed to push {Method} to user {UserId}.", method, userId);
            }
        }
    }
}
