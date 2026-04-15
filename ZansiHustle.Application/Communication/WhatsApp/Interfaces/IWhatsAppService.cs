using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.WhatsApp.Interfaces;

/// <summary>
/// Application-level WhatsApp operations.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Sends a free-form WhatsApp message. Only works inside an active
    /// 24-hour customer service window; otherwise WhatsApp will reject the
    /// message and the provider will surface an error.
    /// </summary>
    Task<Result<string>> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an approved Content API template (HX...) with the given variables.
    /// Required for business-initiated messages.
    /// </summary>
    /// <param name="contentSid">Twilio Content template SID, e.g. <c>HXxxxx</c>.</param>
    /// <param name="contentVariables">JSON-encoded variable map, e.g. <c>{"1":"123456"}</c>.</param>
    Task<Result<string>> SendTemplateAsync(
        string toPhoneNumber,
        string contentSid,
        string? contentVariables,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a one-time password via WhatsApp using the default OTP Content
    /// template (configured as <c>Twilio:WhatsApp:DefaultContentSid</c>).
    /// Falls back to free-form text inside a session window if no template is set.
    /// </summary>
    Task<Result<string>> SendOtpAsync(string toPhoneNumber, string otpCode, CancellationToken cancellationToken = default);
}
