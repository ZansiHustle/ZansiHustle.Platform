using Microsoft.Extensions.Options;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Infrastructure.Communications.Email.Mappers;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Shared.Enums.Communications;

namespace ZansiHustle.API.Services;

/// <summary>
/// Boot-time diagnostic that walks every configured email-sender
/// identity and logs its readiness state ONCE at startup. Runs as an
/// <see cref="IHostedService"/> so it fires after the options system
/// has bound configuration (including environment-variable overrides)
/// but before the app starts taking traffic.
///
/// Why this exists:
/// Previously a missing sender password only surfaced when a user
/// attempted something that triggered that sender (e.g. password
/// reset). Deploy engineers had no way to know their UAT/live
/// environment was missing SMTP secrets until a real user hit the
/// broken path. This reporter surfaces the exact missing fields +
/// the exact env-var names to set, at boot, visibly in the log.
///
/// Safety:
/// Only FIELD NAMES are logged (Host, Port, Username, Password,
/// FromEmail). The reporter never touches or emits secret values.
/// </summary>
public sealed class EmailSenderConfigReporter : IHostedService
{
    private readonly EmailSenderSettings _settings;
    private readonly IHostEnvironment _env;
    private readonly ILogger<EmailSenderConfigReporter> _logger;

    public EmailSenderConfigReporter(
        IOptions<EmailSenderSettings> settings,
        IHostEnvironment env,
        ILogger<EmailSenderConfigReporter> logger)
    {
        _settings = settings.Value ?? new EmailSenderSettings();
        _env = env;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[EmailConfig] Validating email-sender configuration for environment '{Env}'…",
            _env.EnvironmentName);

        // Walk every known EmailSender identity — the enum is the
        // source of truth, so if a new one is added we'll automatically
        // surface it in the report without having to update this file.
        var identities = Enum.GetValues<EmailSender>();
        var totalReady = 0;
        var totalMissing = 0;

        foreach (var id in identities)
        {
            var options = PickByName(id.ToString());
            var valid = EmailSenderMapper.IsValid(options, out var missing);

            if (valid)
            {
                totalReady++;
                _logger.LogInformation(
                    "[EmailConfig] Sender '{Sender}' is READY (Host={Host}, Port={Port}, FromEmail={FromEmail}).",
                    id, options!.Host, options.Port, options.FromEmail);
            }
            else
            {
                totalMissing++;
                var envVars = string.Join(", ", missing.Select(f => $"EmailProviders__Senders__{id}__{f}"));
                _logger.LogWarning(
                    "[EmailConfig] Sender '{Sender}' is INCOMPLETE. Missing: {Missing}. " +
                    "Set env vars: {EnvVars}",
                    id, string.Join(", ", missing), envVars);
            }
        }

        if (totalMissing > 0)
        {
            _logger.LogWarning(
                "[EmailConfig] {Missing}/{Total} email senders are incomplete. " +
                "Password reset and related flows using incomplete senders WILL fail at runtime.",
                totalMissing, totalReady + totalMissing);
        }
        else
        {
            _logger.LogInformation(
                "[EmailConfig] All {Total} email senders are configured.",
                totalReady);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private EmailSenderOptions? PickByName(string senderName) => senderName switch
    {
        nameof(EmailSender.Accounts) => _settings.Accounts,
        nameof(EmailSender.Support)  => _settings.Support,
        nameof(EmailSender.NoReply)  => _settings.NoReply,
        nameof(EmailSender.Security) => _settings.Security,
        nameof(EmailSender.Payments) => _settings.Payments,
        nameof(EmailSender.Default)  => _settings.Default,
        _ => null
    };
}
