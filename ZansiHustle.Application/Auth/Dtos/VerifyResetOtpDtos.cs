using System.ComponentModel.DataAnnotations;

namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Submitted by the Verify-OTP page to exchange a successful OTP
/// attempt for an Identity password-reset token. The returned token
/// feeds into the existing /reset-password endpoint.
/// </summary>
public sealed class VerifyResetOtpRequestDto
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Returned after the submitted OTP is successfully verified. The
/// client stores <see cref="UserId"/> + <see cref="ResetToken"/> in
/// navigation state and passes them to /reset-password in the next
/// step.
///
/// The token is Identity's built-in password-reset token (time-bound,
/// single-use). Treat it like a bearer credential — short TTL only.
/// </summary>
public sealed class VerifyResetOtpResponseDto
{
    public string UserId { get; set; } = string.Empty;
    public string ResetToken { get; set; } = string.Empty;
}
