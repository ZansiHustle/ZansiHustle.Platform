namespace ZansiHustle.Infrastructure.Communications.Twilio;

/// <summary>
/// Root Twilio credentials and channel-specific options.
/// Bound from the "Twilio" configuration section. Do not commit real values;
/// supply them via environment variables or user-secrets (e.g. Twilio__AuthToken).
/// </summary>
public sealed class TwilioSettings
{
    public const string SectionName = "Twilio";

    /// <summary>
    /// Twilio Account SID (found in Twilio Console). Required for both
    /// AuthToken and API-Key auth modes — when API-Key auth is used, the
    /// SDK still needs the parent Account SID for resource paths.
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Auth Token. Used as the fallback authentication mechanism
    /// when no API Key is configured. Keep secret.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Twilio API Key SID (starts with "SK..."). Preferred over AuthToken
    /// because API Keys can be scoped, listed, and revoked independently
    /// without rotating the master Auth Token. Used together with
    /// <see cref="ApiKeySecret"/>.
    /// </summary>
    public string ApiKeySid { get; set; } = string.Empty;

    /// <summary>
    /// Twilio API Key secret paired with <see cref="ApiKeySid"/>. Keep secret.
    /// </summary>
    public string ApiKeySecret { get; set; } = string.Empty;

    /// <summary>
    /// Optional sub-account SID to use instead of the master account.
    /// </summary>
    public string? SubaccountSid { get; set; }

    public TwilioSmsOptions Sms { get; set; } = new();
    public TwilioWhatsAppOptions WhatsApp { get; set; } = new();
    public TwilioVerifyOptions Verify { get; set; } = new();

    /// <summary>
    /// True when API Key + Secret + Account SID are all populated. Preferred
    /// auth path; checked before <see cref="HasAuthTokenCredentials"/>.
    /// </summary>
    public bool HasApiKeyCredentials()
        => !string.IsNullOrWhiteSpace(AccountSid)
        && !string.IsNullOrWhiteSpace(ApiKeySid)
        && !string.IsNullOrWhiteSpace(ApiKeySecret);

    /// <summary>
    /// True when Account SID + Auth Token are populated (legacy auth path).
    /// </summary>
    public bool HasAuthTokenCredentials()
        => !string.IsNullOrWhiteSpace(AccountSid) && !string.IsNullOrWhiteSpace(AuthToken);

    /// <summary>
    /// True when the client can authenticate with at least one mechanism.
    /// </summary>
    public bool HasCredentials() => HasApiKeyCredentials() || HasAuthTokenCredentials();
}

/// <summary>
/// Twilio Verify (V2) options. We use Verify, not raw Programmable SMS, for
/// OTP because Twilio handles code generation, expiry, and check semantics —
/// and it works with Twilio's pooled global senders while we wait for the
/// South African regulatory bundle to issue a local long code.
/// </summary>
public sealed class TwilioVerifyOptions
{
    /// <summary>
    /// Verify Service SID (VA...). Created in Twilio Console → Verify → Services.
    /// </summary>
    public string ServiceSid { get; set; } = string.Empty;

    /// <summary>
    /// Per-destination cooldown enforced on our side BEFORE we hit Twilio.
    /// Twilio also enforces its own rate limits, but a local cooldown is
    /// cheaper and stops obvious spam without burning Verify credits.
    /// </summary>
    public int ResendCooldownSeconds { get; set; } = 30;

    public bool HasService() => !string.IsNullOrWhiteSpace(ServiceSid);
}

/// <summary>
/// SMS-specific Twilio sender options.
/// </summary>
public sealed class TwilioSmsOptions
{
    /// <summary>
    /// E.164 number provisioned in Twilio for outbound SMS (e.g. +27...).
    /// </summary>
    public string FromPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional Messaging Service SID. If set, takes precedence over FromPhoneNumber
    /// and enables features like sender pools and intelligent routing.
    /// </summary>
    public string? MessagingServiceSid { get; set; }

    public bool HasSender()
        => !string.IsNullOrWhiteSpace(FromPhoneNumber) || !string.IsNullOrWhiteSpace(MessagingServiceSid);
}

/// <summary>
/// WhatsApp-specific Twilio sender options.
/// </summary>
public sealed class TwilioWhatsAppOptions
{
    /// <summary>
    /// E.164 WhatsApp-enabled sender (e.g. +14155238886 for sandbox).
    /// Do NOT include the "whatsapp:" prefix here — the provider prepends it.
    /// </summary>
    public string FromPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Optional Messaging Service SID containing WhatsApp-enabled senders.
    /// </summary>
    public string? MessagingServiceSid { get; set; }

    /// <summary>
    /// Optional default Content API template SID (HX...) used for OTP delivery.
    /// Required for production WhatsApp outside the 24-hour session window.
    /// </summary>
    public string? DefaultContentSid { get; set; }

    public bool HasSender()
        => !string.IsNullOrWhiteSpace(FromPhoneNumber) || !string.IsNullOrWhiteSpace(MessagingServiceSid);
}
