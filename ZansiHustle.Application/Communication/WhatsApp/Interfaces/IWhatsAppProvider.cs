using ZansiHustle.Application.Communications.WhatsApp.Models;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.WhatsApp.Interfaces;

/// <summary>
/// Low-level provider contract for sending WhatsApp messages.
/// </summary>
public interface IWhatsAppProvider
{
    /// <summary>
    /// Sends a WhatsApp message through the configured provider.
    /// Returns the provider message SID on success.
    /// </summary>
    Task<Result<string>> SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default);
}
