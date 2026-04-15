namespace ZansiHustle.Application.Communications.WhatsApp.Models;

/// <summary>
/// Represents an outbound WhatsApp message.
///
/// WhatsApp Business policy: to message a user outside the 24-hour customer
/// service window, the message MUST use an approved Content template.
/// Pass <see cref="ContentSid"/> + <see cref="ContentVariables"/> for template
/// sends; pass <see cref="Body"/> for free-form replies inside the session.
/// </summary>
public sealed class WhatsAppMessage
{
    /// <summary>
    /// E.164 phone number of the recipient (without the <c>whatsapp:</c> prefix).
    /// </summary>
    public string ToPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Free-form text body. Used only when sending inside an active
    /// customer-service window.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Twilio Content API template SID (HX...) for approved templated messages.
    /// Required for first-contact / business-initiated messages.
    /// </summary>
    public string? ContentSid { get; set; }

    /// <summary>
    /// JSON-encoded variables map for the Content template, e.g.
    /// <c>{"1":"123456"}</c>.
    /// </summary>
    public string? ContentVariables { get; set; }
}
