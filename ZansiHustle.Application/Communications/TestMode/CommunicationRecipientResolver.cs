using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Communications.Email.Models;

namespace ZansiHustle.Application.Communications.TestMode;

/// <inheritdoc />
public sealed class CommunicationRecipientResolver : ICommunicationRecipientResolver
{
    private readonly CommunicationTestModeSettings _settings;
    private readonly EmailTestModeSettings _legacyEmail;
    private readonly ILogger<CommunicationRecipientResolver> _logger;

    public CommunicationRecipientResolver(
        IOptions<CommunicationTestModeSettings> settings,
        IOptions<EmailTestModeSettings> legacyEmail,
        ILogger<CommunicationRecipientResolver> logger)
    {
        _settings = settings?.Value ?? new CommunicationTestModeSettings();
        _legacyEmail = legacyEmail?.Value ?? new EmailTestModeSettings();
        _logger = logger;
    }

    public string? ResolveEmail(string? realEmail, CommunicationPurpose purpose)
    {
        if (!OverrideAllowed(purpose))
            return Trimmed(realEmail);

        // Preferred: central OverrideEmailTo. Fallback (deprecated): the old
        // shipment-specific EmailTestMode override, used ONLY when the new one
        // is empty — logged with a deprecation warning so it gets migrated.
        var overrideTo = Trimmed(_settings.OverrideEmailTo);
        if (overrideTo is null && !string.IsNullOrWhiteSpace(_legacyEmail.OverrideShipmentEmailsTo))
        {
            overrideTo = _legacyEmail.OverrideShipmentEmailsTo!.Trim();
            _logger.LogWarning(
                "DEPRECATED config EmailTestMode:OverrideShipmentEmailsTo used as fallback for {Purpose}. " +
                "Migrate to CommunicationTestMode:OverrideEmailTo.", purpose);
        }
        if (overrideTo is null)
            return Trimmed(realEmail);

        _logger.LogWarning(
            "Email recipient overridden for test mode (purpose={Purpose}) real={Real} → override={Override}",
            purpose, MaskEmail(realEmail), MaskEmail(overrideTo));
        return overrideTo;
    }

    public string? ResolveSms(string? realPhone, CommunicationPurpose purpose)
    {
        if (!OverrideAllowed(purpose))
            return Trimmed(realPhone);

        var overrideTo = Trimmed(_settings.OverrideSmsTo);
        if (overrideTo is null)
            return Trimmed(realPhone);

        _logger.LogWarning(
            "SMS recipient overridden for test mode (purpose={Purpose}) real={Real} → override={Override}",
            purpose, MaskPhone(realPhone), MaskPhone(overrideTo));
        return overrideTo;
    }

    public string? ResolveWhatsApp(string? realWhatsApp, CommunicationPurpose purpose)
    {
        if (!OverrideAllowed(purpose))
            return Trimmed(realWhatsApp);

        var overrideTo = Trimmed(_settings.OverrideWhatsAppTo);
        if (overrideTo is null)
            return Trimmed(realWhatsApp);

        _logger.LogWarning(
            "WhatsApp recipient overridden for test mode (purpose={Purpose}) real={Real} → override={Override}",
            purpose, MaskPhone(realWhatsApp), MaskPhone(overrideTo));
        return overrideTo;
    }

    /// <summary>Overriding is allowed only when test mode is on AND, for security
    /// purposes, only when explicitly permitted. Default-safe.</summary>
    private bool OverrideAllowed(CommunicationPurpose purpose)
    {
        if (!_settings.Enabled) return false;
        if (IsSecurity(purpose)) return _settings.OverrideSecurityOtpRecipients;
        return true;
    }

    private static bool IsSecurity(CommunicationPurpose p) =>
        p is CommunicationPurpose.SecurityOtp
          or CommunicationPurpose.LoginOtp
          or CommunicationPurpose.PasswordReset;

    private static string? Trimmed(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "<none>";
        var at = email.IndexOf('@');
        if (at <= 0) return "***";
        return $"{email[0]}***{email.Substring(at)}";
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "<none>";
        var digits = phone.Trim();
        return digits.Length <= 4 ? "***" : $"***{digits.Substring(digits.Length - 4)}";
    }
}
