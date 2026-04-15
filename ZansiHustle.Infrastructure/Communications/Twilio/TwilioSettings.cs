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
    /// Twilio Account SID (found in Twilio Console).
    /// </summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Auth Token. Keep secret.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional sub-account SID to use instead of the master account.
    /// </summary>
    public string? SubaccountSid { get; set; }

    public TwilioSmsOptions Sms { get; set; } = new();
    public TwilioWhatsAppOptions WhatsApp { get; set; } = new();

    public bool HasCredentials()
        => !string.IsNullOrWhiteSpace(AccountSid) && !string.IsNullOrWhiteSpace(AuthToken);
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
