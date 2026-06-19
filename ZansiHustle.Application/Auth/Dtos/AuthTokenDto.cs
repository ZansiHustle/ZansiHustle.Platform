namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Represents issued authentication tokens.
/// </summary>
public sealed class AuthTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public Guid UserId { get; set; }
    public CurrentUserDto User { get; set; } = new();

    /// <summary>
    /// True when the credentials were valid but the account still needs OTP
    /// verification before a session is issued. When true, AccessToken /
    /// RefreshToken are EMPTY (no session granted) and the client must route the
    /// user to the OTP verification screen. False on every normal login.
    /// </summary>
    public bool RequiresVerification { get; set; }

    /// <summary>
    /// The phone number the client should verify / resend the OTP to. Only
    /// populated when <see cref="RequiresVerification"/> is true.
    /// </summary>
    public string? VerificationPhoneNumber { get; set; }

    /// <summary>
    /// The account email the client can verify via email OTP. Only populated
    /// when <see cref="RequiresVerification"/> is true. Lets the client offer
    /// the Email channel (in addition to SMS/WhatsApp) on the verify screen.
    /// </summary>
    public string? VerificationEmail { get; set; }
}