namespace ZansiHustle.Application.Auth;

/// <summary>
/// QA / staging OTP bypass options. Bound from the "Auth:TestMode"
/// configuration section.
///
/// <para>
/// When <see cref="Enabled"/> is <c>true</c> the OTP verification
/// pipeline accepts the literal code <see cref="BypassCode"/>
/// (default <c>"111111"</c>) for any well-formed verification attempt.
/// Real OTP validation continues to work in parallel — only the
/// "match the bypass code" branch is added.
/// </para>
///
/// <para>
/// Default is <c>false</c>. **Never enable in production.** The flag
/// is read by:
/// <list type="bullet">
///   <item><c>TwilioVerifyService.VerifyOtpAsync</c> — phone OTPs</item>
///   <item><c>OtpService.VerifyAsync</c> — email/SMS/WhatsApp OTPs
///         that flow through our own session store</item>
/// </list>
/// Both implementations log <c>[TEST_MODE] OTP bypass accepted</c>
/// (without the code) so any non-prod environment with the flag on
/// is obvious in the log stream.
/// </para>
/// </summary>
public sealed class AuthTestModeSettings
{
    public const string SectionName = "Auth:TestMode";

    /// <summary>
    /// Master switch. <c>false</c> by default — real OTP only.
    /// Override in non-prod environments via env var
    /// <c>Auth__TestMode__Enabled=true</c>.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The literal code accepted when <see cref="Enabled"/> is on.
    /// Defaults to <c>"111111"</c>; can be overridden per environment
    /// to make the bypass less guessable for shared QA boxes.
    /// </summary>
    public string BypassCode { get; set; } = "111111";
}
