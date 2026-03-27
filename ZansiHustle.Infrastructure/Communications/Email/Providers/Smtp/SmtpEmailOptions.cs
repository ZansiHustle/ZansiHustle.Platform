namespace ZansiHustle.Infrastructure.Communications.Email.Providers.Smtp;

/// <summary>
/// SMTP configuration options.
/// </summary>
public sealed class SmtpEmailOptions
{
    public const string SectionName = "EmailProviders:Smtp";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// Username used for SMTP authentication.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password used for SMTP authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Default sender email address.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default sender display name.
    /// </summary>
    public string FromName { get; set; } = "ZansiHustle";

    /// <summary>
    /// Enables SSL.
    /// </summary>
    public bool EnableSsl { get; set; } = true;
}