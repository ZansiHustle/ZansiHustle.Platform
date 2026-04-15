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
            _logger.LogWarning("Twilio credentials are not configured. SMS/WhatsApp sending will be disabled.");
            return null;
        }

        var effectiveAccountSid = string.IsNullOrWhiteSpace(_settings.SubaccountSid)
            ? _settings.AccountSid
            : _settings.SubaccountSid;

        return new TwilioRestClient(
            username: _settings.AccountSid,
            password: _settings.AuthToken,
            accountSid: effectiveAccountSid,
            httpClient: null,
            region: null);
    }
}
