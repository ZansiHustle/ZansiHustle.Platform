using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZansiHustle.Application.Notifications
{
    /// <summary>
    /// Sends a push notification to a user's registered devices. Abstraction so
    /// the rest of the app never depends on OneSignal directly. Implementations
    /// MUST be best-effort: a push failure (provider down, no keys, no device)
    /// must never throw into — or fail — the calling business transaction.
    /// <see cref="NullPushNotificationService"/> is the safe no-op fallback used
    /// when OneSignal isn't configured.
    /// </summary>
    public interface IPushNotificationService
    {
        Task SendToUserAsync(
            Guid userId,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null);
    }
}
