using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Communications.Email.Mappers;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Communications.Email.Mappers;

/// <summary>
/// Resolves an <see cref="EmailSender"/> identity to the configured
/// <see cref="EmailSenderOptions"/> loaded from <c>EmailProviders:Senders</c>.
/// Falls back to the <c>Default</c> sender when a specific one is not configured.
/// </summary>
public sealed class EmailSenderMapper : IEmailSenderMapper
{
    private readonly EmailSenderSettings _settings;
    private readonly ILogger<EmailSenderMapper> _logger;

    public EmailSenderMapper(IOptions<EmailSenderSettings> settings, ILogger<EmailSenderMapper> logger)
    {
        _settings = settings.Value ?? new EmailSenderSettings();
        _logger = logger;
    }

    public Task<Result<EmailSenderOptions>> MapAsync(EmailSender sender)
    {
        var options = Pick(sender) ?? _settings.Default;

        if (options is null || !IsValid(options))
        {
            _logger.LogError(
                "Email sender {Sender} is not configured (or missing required fields). " +
                "Check EmailProviders:Senders in configuration.", sender);

            return Task.FromResult(Result<EmailSenderOptions>.Failure(
                ErrorCodes.Exception,
                $"Email sender '{sender}' is not configured."));
        }

        return Task.FromResult(Result<EmailSenderOptions>.Success(options));
    }

    private EmailSenderOptions? Pick(EmailSender sender) => sender switch
    {
        EmailSender.Accounts => _settings.Accounts,
        EmailSender.Support => _settings.Support,
        EmailSender.NoReply => _settings.NoReply,
        EmailSender.Security => _settings.Security,
        EmailSender.Payments => _settings.Payments,
        EmailSender.Default => _settings.Default,
        _ => null
    };

    private static bool IsValid(EmailSenderOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.Host)
            && options.Port > 0
            && !string.IsNullOrWhiteSpace(options.Username)
            && !string.IsNullOrWhiteSpace(options.Password)
            && !string.IsNullOrWhiteSpace(options.FromEmail);
    }
}
