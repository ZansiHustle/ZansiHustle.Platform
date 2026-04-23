using ZansiHustle.Application.Communications.Email.Models;

namespace ZansiHustle.Infrastructure.Configuration;

/// <summary>
/// Root options for outbound email sender identities. Bound from the
/// <c>EmailProviders:Senders</c> configuration section.
///
/// <para>
/// <b>Secret handling</b> — never commit SMTP passwords to source control:
/// <list type="bullet">
///   <item><description><c>appsettings.json</c> holds structure and non-secret defaults only.</description></item>
///   <item><description><c>appsettings.Development.json</c> holds dev-only secrets (local machine).</description></item>
///   <item><description><c>UAT / Live</c> inject secrets via environment variables or hosting config.</description></item>
/// </list>
/// Startup validation is performed by <c>EmailSenderConfigReporter</c>,
/// which logs — per sender — whether it is READY or INCOMPLETE and
/// exactly which env-var names need to be set.
/// </para>
///
/// <para>
/// <b>Required environment variables for UAT / Live</b>
/// (.NET's configuration system maps <c>__</c> to nested keys, so
/// these override the corresponding JSON values at runtime):
/// <code>
/// EmailProviders__Senders__Default__Password
/// EmailProviders__Senders__Accounts__Password
/// EmailProviders__Senders__Support__Password
/// EmailProviders__Senders__NoReply__Password
/// EmailProviders__Senders__Security__Password    ← used by password-reset OTP emails
/// EmailProviders__Senders__Payments__Password
/// </code>
/// Host / Port / Username / FromEmail / FromName / EnableSsl are kept
/// in <c>appsettings.json</c> (non-secret). Only the password field is
/// expected to come from the environment in production.
/// </para>
///
/// <para>
/// <b>Optional</b> — any of the non-secret fields can also be
/// overridden per environment using the same pattern, e.g.
/// <c>EmailProviders__Senders__Security__FromEmail</c>.
/// </para>
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
