using ZansiHustle.Application.Communications.Email.Models;

namespace ZansiHustle.Infrastructure.Configuration;

/// <summary>
/// Root options for outbound email sender identities.
/// Bound from the "EmailProviders:Senders" configuration section.
/// Never commit real SMTP passwords; supply them via user-secrets, appsettings
/// overrides, or environment variables (e.g. EmailProviders__Senders__NoReply__Password).
/// </summary>
public sealed class EmailSenderSettings
{
    public const string SectionName = "EmailProviders:Senders";

    public EmailSenderOptions? Default { get; set; }
    public EmailSenderOptions? Accounts { get; set; }
    public EmailSenderOptions? Support { get; set; }
    public EmailSenderOptions? NoReply { get; set; }
    public EmailSenderOptions? Security { get; set; }
    public EmailSenderOptions? Payments { get; set; }
}
