namespace ZansiHustle.Application.Communications.TestMode;

/// <summary>
/// CENTRAL dev/UAT communication test-mode switch. Bound from the
/// <c>CommunicationTestMode</c> configuration section. When <see cref="Enabled"/>
/// is true, outbound recipients for NON-security communications are re-routed to
/// the configured override addresses so QA can verify on a single inbox/number.
///
/// SECURITY: defaults are all empty/false (production behaviour). Security OTP /
/// login / password-reset recipients are overridden ONLY when
/// <see cref="OverrideSecurityOtpRecipients"/> is explicitly true. NEVER enable
/// in production. Env vars: <c>CommunicationTestMode__Enabled</c>,
/// <c>CommunicationTestMode__OverrideEmailTo</c>,
/// <c>CommunicationTestMode__OverrideSmsTo</c>,
/// <c>CommunicationTestMode__OverrideWhatsAppTo</c>,
/// <c>CommunicationTestMode__OverrideSecurityOtpRecipients</c>.
/// </summary>
public sealed class CommunicationTestModeSettings
{
    public const string SectionName = "CommunicationTestMode";

    /// <summary>Master switch. False = real recipients (production).</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>When set + enabled, all non-security emails go here.</summary>
    public string? OverrideEmailTo { get; set; }

    /// <summary>When set + enabled, all non-security SMS go here (E.164).</summary>
    public string? OverrideSmsTo { get; set; }

    /// <summary>When set + enabled, all non-security WhatsApp messages go here (E.164).</summary>
    public string? OverrideWhatsAppTo { get; set; }

    /// <summary>Allow overriding SECURITY recipients (OTP/login/password-reset).
    /// Default FALSE — must be explicitly enabled. Keep false unless you really
    /// intend to capture auth codes in test mode.</summary>
    public bool OverrideSecurityOtpRecipients { get; set; } = false;
}
