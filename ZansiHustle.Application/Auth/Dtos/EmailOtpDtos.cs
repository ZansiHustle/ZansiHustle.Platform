namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>Request a one-time account-verification code by email.</summary>
public class SendEmailOtpRequestDto
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>Verify an account-verification email OTP (session + code).</summary>
public class VerifyEmailOtpRequestDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Session envelope returned after an account-verification email OTP is issued.
/// Mirrors the password-reset session shape so the client can drive the same
/// "enter the code" UI. <see cref="EmailMasked"/> is presentational only.
/// </summary>
public sealed class EmailOtpSessionDto
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int CodeLength { get; set; }
    public int ResendCooldownSeconds { get; set; }
    public string EmailMasked { get; set; } = string.Empty;
}

/// <summary>Result of verifying an account-verification email OTP.</summary>
public sealed class VerifyEmailOtpResponseDto
{
    public bool Verified { get; set; }
}
