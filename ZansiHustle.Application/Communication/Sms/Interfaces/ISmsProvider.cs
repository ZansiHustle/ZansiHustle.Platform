using ZansiHustle.Application.Communications.Sms.Models;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Sms.Interfaces;

/// <summary>
/// Low-level provider contract for sending SMS messages.
/// </summary>
public interface ISmsProvider
{
    /// <summary>
    /// Sends an SMS message through the configured provider.
    /// Returns the provider message SID on success.
    /// </summary>
    Task<Result<string>> SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}
