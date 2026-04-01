using ZansiHustle.Shared.Enums.Communications;

namespace ZansiHustle.Application.Communications.Email.Models;

/// <summary>
/// Represents an email message ready for provider delivery.
/// </summary>
public sealed class EmailMessage
{
    /// <summary>
    /// Primary recipient email address.
    /// </summary>
    public string ToEmail { get; set; } = string.Empty;

    /// <summary>
    /// Optional recipient display name.
    /// </summary>
    public string? ToName { get; set; }

    /// <summary>
    /// Email subject line.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML email body.
    /// </summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>
    /// Plain-text email body.
    /// </summary>
    public string PlainTextBody { get; set; } = string.Empty;

    /// <summary>
    /// Optional reply-to email address.
    /// </summary>
    public string? ReplyToEmail { get; set; }

    /// <summary>
    /// Optional CC email address.
    /// </summary>
    public List<string>? CcEmails { get; set; } = null;

    /// <summary>
    /// Indicates which configured sender identity should be used.
    /// </summary>
    public EmailSender Sender { get; set; } = EmailSender.Default;
}