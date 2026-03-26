namespace ZansiHustle.Application.Common.Interfaces;

/// <summary>
/// Defines email-sending operations used by the application layer.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email message asynchronously.
    /// </summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}