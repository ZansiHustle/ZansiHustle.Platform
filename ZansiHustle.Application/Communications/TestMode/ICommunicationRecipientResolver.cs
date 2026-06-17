namespace ZansiHustle.Application.Communications.TestMode;

/// <summary>
/// Central resolver for the ACTUAL recipient of an outbound communication.
/// Call sites pass the REAL recipient + the <see cref="CommunicationPurpose"/>;
/// in dev/UAT test mode this may return an override address. Call sites must NOT
/// read test-mode config directly — they always go through this resolver.
///
/// Security purposes are never overridden unless explicitly allowed (see
/// <see cref="CommunicationTestModeSettings.OverrideSecurityOtpRecipients"/>).
/// All overrides are logged with masked values + the purpose.
/// </summary>
public interface ICommunicationRecipientResolver
{
    /// <summary>Resolve the email recipient (override in test mode, else the real address; may be null).</summary>
    string? ResolveEmail(string? realEmail, CommunicationPurpose purpose);

    /// <summary>Resolve the SMS recipient (override in test mode, else the real number; may be null).</summary>
    string? ResolveSms(string? realPhone, CommunicationPurpose purpose);

    /// <summary>Resolve the WhatsApp recipient (override in test mode, else the real number; may be null).</summary>
    string? ResolveWhatsApp(string? realWhatsApp, CommunicationPurpose purpose);
}
