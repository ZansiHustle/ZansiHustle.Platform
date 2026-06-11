using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Notifications;

namespace ZansiHustle.Infrastructure.Notifications.Push
{
    /// <summary>
    /// Safe no-op push transport — used when OneSignal isn't configured. In-app
    /// notifications (REST + SignalR) keep working; only the device push is
    /// skipped, with a dev-level log so it's visible without being noisy.
    /// </summary>
    public sealed class NullPushNotificationService : IPushNotificationService
    {
        private readonly ILogger<NullPushNotificationService> _logger;

        public NullPushNotificationService(ILogger<NullPushNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendToUserAsync(
            Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null)
        {
            _logger.LogDebug(
                "[Push][Null] Skipped push to user {UserId} (OneSignal not configured). title={Title}",
                userId, title);
            return Task.CompletedTask;
        }
    }
}
