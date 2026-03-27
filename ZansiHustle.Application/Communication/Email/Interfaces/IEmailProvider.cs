using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Email.Interfaces;

/// <summary>
/// Defines the low-level provider contract for delivering email messages.
/// </summary>
public interface IEmailProvider
{
    /// <summary>
    /// Sends an email message using the configured provider.
    /// </summary>
    /// <param name="message">The email message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}