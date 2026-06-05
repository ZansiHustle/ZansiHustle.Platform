using Microsoft.Extensions.Options;
using ZansiHustle.Infrastructure.Configuration;

namespace ZansiHustle.API.Services;

/// <summary>
/// Boot-time diagnostic for the Ozow integration. Logs once at startup
/// whether the credentials and URLs are present and whether any URL is
/// pointing at localhost (which would be unreachable from Ozow's network).
///
/// Why this exists:
/// All ZansiHustle development hits the deployed UAT API at
/// https://uatapi.zansihustle.com/ — there is no local API instance.
/// Ozow webhook delivery requires a publicly reachable HTTPS endpoint.
/// A misconfigured deployment (missing PrivateKey, NotifyUrl pointing to
/// localhost, etc.) previously only surfaced when a buyer hit "Pay" and
/// the call to Ozow failed, or worse, when a webhook was silently lost.
/// This reporter surfaces those gaps at boot, in the log, with the exact
/// env-var names to set.
///
/// Safety:
/// Only field NAMES are logged. Never ApiKey or PrivateKey values.
/// </summary>
public sealed class OzowConfigReporter : IHostedService
{
    private readonly OzowSettings _settings;
    private readonly IHostEnvironment _env;
    private readonly ILogger<OzowConfigReporter> _logger;

    public OzowConfigReporter(
        IOptions<OzowSettings> settings,
        IHostEnvironment env,
        ILogger<OzowConfigReporter> logger)
    {
        _settings = settings.Value ?? new OzowSettings();
        _env = env;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[OzowConfig] Validating Ozow configuration for environment '{Env}'…",
            _env.EnvironmentName);

        // ── Production guard: UAT TEST MODE must NEVER be on in Production ──
        // This is a hard fail rather than a warning. The whole point of
        // UatTestMode is to cap real-money charges at R5 for safe live-bank
        // testing. Leaving it on in Production would silently turn every
        // checkout into an R5 charge, which would be a far worse outage
        // than refusing to boot. Fail loud, fail early.
        if (_env.IsProduction() && _settings.UatTestMode)
        {
            const string msg =
                "[OzowConfig] FATAL: Ozow:UatTestMode is true but the host environment is Production. " +
                "UatTestMode caps every charged amount and would corrupt live takings. " +
                "Set Ozow__UatTestMode=false (or remove it) on the Production host and redeploy.";
            _logger.LogCritical(msg);
            throw new InvalidOperationException(msg);
        }

        // ── Credentials + NotifyUrl ──────────────────────────────────────────
        var missing = _settings.GetMissingFieldEnvVars();

        if (missing.Count > 0)
        {
            _logger.LogWarning(
                "[OzowConfig] Ozow integration is INCOMPLETE. Missing required fields: {Missing}. " +
                "Set these env vars on the host: {EnvVars}. " +
                "Until set, /api/payments/initialize for Ozow returns PROVIDER_NOT_CONFIGURED.",
                string.Join(", ", missing), string.Join(", ", missing));
        }
        else
        {
            _logger.LogInformation(
                "[OzowConfig] Ozow credentials present (ApiKey, SiteCode, PrivateKey, NotifyUrl). " +
                "BaseUrl={BaseUrl}, IsTest={IsTest}.",
                _settings.BaseUrl, _settings.IsTest);
        }

        // ── Localhost URL guard ──────────────────────────────────────────────
        // All testing happens against the deployed UAT API; localhost URLs
        // would prevent Ozow from delivering webhooks or redirecting users.
        WarnIfLocalhost(nameof(_settings.SuccessUrl), _settings.SuccessUrl);
        WarnIfLocalhost(nameof(_settings.CancelUrl),  _settings.CancelUrl);
        WarnIfLocalhost(nameof(_settings.ErrorUrl),   _settings.ErrorUrl);
        WarnIfLocalhost(nameof(_settings.NotifyUrl),  _settings.NotifyUrl);
        WarnIfLocalhost(nameof(_settings.BaseUrl),    _settings.BaseUrl);

        // ── HTTPS guard for NotifyUrl ────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(_settings.NotifyUrl)
            && !_settings.NotifyUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "[OzowConfig] NotifyUrl is not HTTPS ({Url}). Ozow delivers webhooks to HTTPS endpoints only.",
                _settings.NotifyUrl);
        }

        // ── UAT controlled-testing announcement ──────────────────────────────
        if (_settings.UatTestMode)
        {
            if (_settings.UatTestAmount <= 0m)
            {
                _logger.LogError(
                    "[OzowConfig] UatTestMode is ON but UatTestAmount is {Amount}. " +
                    "Set a positive Ozow__UatTestAmount (e.g. 5.00) before testing.",
                    _settings.UatTestAmount);
            }
            else
            {
                _logger.LogWarning(
                    "[OzowConfig] UAT TEST MODE is ON. Every Ozow payment will be capped at R{Cap} " +
                    "and tagged with UAT-TEST-/IsTest=true. Disable Ozow:UatTestMode for production.",
                    _settings.UatTestAmount);
            }
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
                "[OzowConfig] Ozow:{Field} contains a localhost address ({Url}). " +
                "Ozow runs on the public internet and will not be able to reach this. " +
                "Use https://uatapi.zansihustle.com/ for UAT.",
                fieldName, url);
        }
    }
}
