using Microsoft.Extensions.Options;
using ZansiHustle.Infrastructure.Configuration;

namespace ZansiHustle.API.Services;

/// <summary>
/// Boot-time diagnostic for the Yoco integration. Mirrors
/// <see cref="OzowConfigReporter"/> — logs once at startup whether the
/// Yoco SecretKey + WebhookSigningSecret are present, warns on localhost
/// URLs, and announces UAT TEST MODE when on. Field NAMES only — never
/// echoes the secrets themselves.
/// </summary>
public sealed class YocoConfigReporter : IHostedService
{
    private readonly YocoSettings _settings;
    private readonly IHostEnvironment _env;
    private readonly ILogger<YocoConfigReporter> _logger;

    public YocoConfigReporter(
        IOptions<YocoSettings> settings,
        IHostEnvironment env,
        ILogger<YocoConfigReporter> logger)
    {
        _settings = settings.Value ?? new YocoSettings();
        _env = env;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[YocoConfig] Validating Yoco configuration for environment '{Env}'…",
            _env.EnvironmentName);

        var missing = _settings.GetMissingFieldEnvVars();

        if (missing.Count > 0)
        {
            _logger.LogWarning(
                "[YocoConfig] Yoco integration is INCOMPLETE. Missing required fields: {Missing}. " +
                "Set these env vars on the host: {EnvVars}. " +
                "Until set, /api/payments/initialize for Yoco returns PROVIDER_NOT_CONFIGURED.",
                string.Join(", ", missing), string.Join(", ", missing));
        }
        else
        {
            _logger.LogInformation(
                "[YocoConfig] Yoco credentials present (SecretKey, WebhookSigningSecret). " +
                "BaseUrl={BaseUrl}.",
                _settings.BaseUrl);
        }

        WarnIfLocalhost(nameof(_settings.SuccessUrl), _settings.SuccessUrl);
        WarnIfLocalhost(nameof(_settings.CancelUrl),  _settings.CancelUrl);
        WarnIfLocalhost(nameof(_settings.FailureUrl), _settings.FailureUrl);
        WarnIfLocalhost(nameof(_settings.BaseUrl),    _settings.BaseUrl);

        if (_settings.UatTestMode)
        {
            _logger.LogWarning(
                "[YocoConfig] UAT TEST MODE is ON. Every Yoco payment will be capped at R{Cap} " +
                "and tagged with UAT-TEST-/IsTest=true. Disable Yoco:UatTestMode for production.",
                _settings.UatTestAmount);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void WarnIfLocalhost(string fieldName, string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        if (url.Contains("localhost", StringComparison.OrdinalIgnoreCase)
            || url.Contains("127.0.0.1", StringComparison.Ordinal)
            || url.Contains("0.0.0.0", StringComparison.Ordinal))
        {
            _logger.LogError(
                "[YocoConfig] Yoco:{Field} contains a localhost address ({Url}). " +
                "Yoco runs on the public internet and will not be able to reach this. " +
                "Use https://uatapi.zansihustle.com/ for UAT.",
                fieldName, url);
        }
    }
}
