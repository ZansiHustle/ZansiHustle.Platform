using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Http;

namespace ZansiHustle.Infrastructure.Communications.Twilio;

/// <summary>
/// Abstracts access to a lazily-initialised Twilio REST client so the rest of
/// the codebase never talks to Twilio's static <c>TwilioClient</c> facade.
/// Returns <c>null</c> when credentials are not configured, allowing providers
/// to fail with a clear "not configured" error instead of throwing at startup.
/// </summary>
public interface ITwilioClientProvider
{
    ITwilioRestClient? GetClient();
    bool IsConfigured { get; }
}

public sealed class TwilioClientProvider : ITwilioClientProvider
{
    private readonly TwilioSettings _settings;
    private readonly ILogger<TwilioClientProvider> _logger;
    private readonly Lazy<ITwilioRestClient?> _client;

    public TwilioClientProvider(IOptions<TwilioSettings> options, ILogger<TwilioClientProvider> logger)
    {
        _settings = options.Value ?? new TwilioSettings();
        _logger = logger;
        _client = new Lazy<ITwilioRestClient?>(BuildClient, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public bool IsConfigured => _settings.HasCredentials();

    public ITwilioRestClient? GetClient() => _client.Value;

    private ITwilioRestClient? BuildClient()
    {
        if (!_settings.HasCredentials())
        {
            _logger.LogWarning("Twilio credentials are not configured. SMS/WhatsApp/Verify will be disabled.");
            return null;
        }

        var effectiveAccountSid = string.IsNullOrWhiteSpace(_settings.SubaccountSid)
            ? _settings.AccountSid
            : _settings.SubaccountSid;

        // Prefer API Key + Secret when configured. API Keys can be scoped
        // and revoked independently of the master Auth Token, which is the
        // recommended posture for production. The SDK's Basic-Auth-style
        // constructor accepts (apiKeySid, apiKeySecret, accountSid) just
        // like (accountSid, authToken, accountSid) — only the credential
        // pair changes; the resource path still uses the Account SID.
        string username;
        string password;
        string authMode;

        if (_settings.HasApiKeyCredentials())
        {
            username = _settings.ApiKeySid;
            password = _settings.ApiKeySecret;
            authMode = "ApiKey";
        }
        else
        {
            username = _settings.AccountSid;
            password = _settings.AuthToken;
            authMode = "AuthToken";
        }

        // Log the auth mode only — never the credential values.
        _logger.LogInformation("Twilio client initialised. AuthMode={AuthMode}.", authMode);

        return new TwilioRestClient(
            username: username,
            password: password,
            accountSid: effectiveAccountSid,
            httpClient: null,
            region: null);
    }
}
