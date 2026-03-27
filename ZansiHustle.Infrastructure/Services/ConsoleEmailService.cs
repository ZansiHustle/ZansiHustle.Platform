using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Common.Interfaces;
using ZansiHustle.Application.Communications.Email.Interfaces;

namespace ZansiHustle.Infrastructure.Services;

/// <summary>
/// Development email service that logs email contents instead of sending them.
/// Replace with SMTP/SendGrid/etc. in production.
/// </summary>
public sealed class ConsoleEmailService //: IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "EMAIL OUTBOUND -> To: {ToEmail}, Subject: {Subject}, Body: {Body}",
            toEmail,
            subject,
            htmlBody);

        return Task.CompletedTask;
    }
}