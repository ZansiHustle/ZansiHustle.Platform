using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Notifications;
using ZansiHustle.Application.Persistence.Notifications;

namespace ZansiHustle.Infrastructure.Notifications.Push
{
    /// <summary>
    /// OneSignal REST push transport. Best-effort by contract: every failure
    /// (no config, no device, provider error, network) is swallowed + logged so
    /// it can never break the booking/payment/notification transaction that
    /// triggered it. Looks up the user's active player ids and posts a
    /// targeted notification.
    /// </summary>
    public sealed class OneSignalPushNotificationService : IPushNotificationService
    {
        private const string ApiUrl = "https://onesignal.com/api/v1/notifications";

        private readonly HttpClient _http;
        private readonly INotificationDeviceRepository _devices;
        private readonly OneSignalOptions _options;
        private readonly ILogger<OneSignalPushNotificationService> _logger;

        public OneSignalPushNotificationService(
            HttpClient http,
            INotificationDeviceRepository devices,
            IOptions<OneSignalOptions> options,
            ILogger<OneSignalPushNotificationService> logger)
        {
            _http = http;
            _devices = devices;
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendToUserAsync(
            Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null)
        {
            try
            {
                if (!_options.IsConfigured)
                {
                    _logger.LogDebug("[Push][OneSignal] Skipped — not configured. user={UserId}", userId);
                    return;
                }

                var devices = await _devices.GetActiveForUserAsync(userId);
                var playerIds = devices
                    .Select(d => d.PlayerId)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct()
                    .ToList();

                if (playerIds.Count == 0)
                {
                    _logger.LogDebug("[Push][OneSignal] No active devices for user {UserId}.", userId);
                    return;
                }

                var payload = new Dictionary<string, object?>
                {
                    ["app_id"] = _options.AppId,
                    ["include_player_ids"] = playerIds,
                    ["headings"] = new { en = title },
                    ["contents"] = new { en = body },
                };
                if (data is { Count: > 0 })
                    payload["data"] = data;

                using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
                {
                    Content = JsonContent.Create(payload)
                };
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Basic", _options.RestApiKey);

                var response = await _http.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "[Push][OneSignal] Non-success {Status} sending to user {UserId}.",
                        (int)response.StatusCode, userId);
                }
            }
            catch (Exception ex)
            {
                // Never throw into the caller — push is best-effort.
                _logger.LogError(ex, "[Push][OneSignal] Failed sending to user {UserId}.", userId);
            }
        }
    }
}
