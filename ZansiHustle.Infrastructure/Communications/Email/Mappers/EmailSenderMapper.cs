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
/// Falls back to the <c>Default</c> sender when a specific one is not
/// configured, emitting a WARN log so the fallback is never silent.
///
/// Diagnostics policy:
///   • When a required field is missing we log the exact field name
///     (Host / Port / Username / Password / FromEmail) and the env-var
///     the deploy engineer should set. We NEVER log the secret value.
///   • Validation failures return a Result.Failure that propagates the
///     sender identity (not the secret) to the caller.
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
        var options = Pick(sender);

        // Explicit-fallback step (was silent before). If the requested
        // sender's config block is absent AND Default is valid, fall
        // back — but log a WARNING so the sender-identity mismatch
        // never passes unnoticed in prod logs. Security/Payments
        // emails should really go out via their branded sender.
        if (options is null && sender != EmailSender.Default && IsValid(_settings.Default, out _))
        {
            _logger.LogWarning(
                "Email sender '{Sender}' is not configured — falling back to 'Default'. " +
                "Set EmailProviders__Senders__{Sender}__* env vars in production to use the branded sender.",
                sender, sender);
            options = _settings.Default;
        }

        if (options is null)
        {
            _logger.LogError(
                "Email sender '{Sender}' is not configured and no Default sender is available. " +
                "Configure EmailProviders:Senders:{Sender} (e.g. env var " +
                "EmailProviders__Senders__{Sender}__Password).", sender, sender, sender);

            return Task.FromResult(Result<EmailSenderOptions>.Failure(
                ErrorCodes.Exception,
                $"Email sender '{sender}' is not configured."));
        }

        if (!IsValid(options, out var missing))
        {
            // The missing-fields list is safe to log — it only contains
            // field names like "Password", "Host". Never the values.
            _logger.LogError(
                "Email sender '{Sender}' is missing required fields: {Missing}. " +
                "Set env vars: {EnvVars}",
                sender,
                string.Join(", ", missing),
                string.Join(", ", missing.Select(f => $"EmailProviders__Senders__{sender}__{f}")));

            return Task.FromResult(Result<EmailSenderOptions>.Failure(
                ErrorCodes.Exception,
                $"Email sender '{sender}' is not configured."));
        }

        return Task.FromResult(Result<EmailSenderOptions>.Success(options));
    }

    private EmailSenderOptions? Pick(EmailSender sender) => sender switch
    {
        EmailSender.Accounts => _settings.Accounts,
        EmailSender.Support  => _settings.Support,
        EmailSender.NoReply  => _settings.NoReply,
        EmailSender.Security => _settings.Security,
        EmailSender.Payments => _settings.Payments,
        EmailSender.Default  => _settings.Default,
        _ => null
    };

    /// <summary>
    /// Validates a sender config block. Out-parameter carries the list
    /// of missing REQUIRED field names so callers can log them without
    /// dumping the entire options object (which would include the
    /// password).
    /// </summary>
    public static bool IsValid(EmailSenderOptions? options, out IReadOnlyList<string> missing)
    {
        if (options is null)
        {
            missing = new[] { "Host", "Port", "Username", "Password", "FromEmail" };
            return false;
        }

        var missingList = new List<string>(5);
        if (string.IsNullOrWhiteSpace(options.Host))     missingList.Add("Host");
        if (options.Port <= 0)                           missingList.Add("Port");
        if (string.IsNullOrWhiteSpace(options.Username)) missingList.Add("Username");
        if (string.IsNullOrWhiteSpace(options.Password)) missingList.Add("Password");
        if (string.IsNullOrWhiteSpace(options.FromEmail))missingList.Add("FromEmail");

        missing = missingList;
        return missingList.Count == 0;
    }
}
