namespace ZansiHustle.Application.Communications.Sms.Models;

/// <summary>
/// Represents an outbound SMS message.
/// </summary>
public sealed class SmsMessage
{
    /// <summary>
    /// E.164 phone number of the recipient (e.g. +27821234567).
    /// </summary>
    public string ToPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Plain-text message body.
    /// </summary>
    public string Body { get; set; } = string.Empty;
}
