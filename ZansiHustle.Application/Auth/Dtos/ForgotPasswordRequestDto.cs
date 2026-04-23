using System.ComponentModel.DataAnnotations;

namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Forgot-password request payload. The user picks a delivery channel
/// and supplies either an email address or a phone number in
/// <see cref="EmailOrPhone"/> (channel-appropriate).
///
/// Channel is a string ("email" or "sms") rather than an enum to keep
/// the public API consumer-friendly; AuthService maps it to the
/// internal <c>OtpChannel</c> enum.
/// </summary>
public sealed class ForgotPasswordRequestDto
{
    /// <summary>
    /// Email address (when Channel == "email") or phone number in
    /// international E.164 form (when Channel == "sms").
    /// </summary>
    [Required]
    public string EmailOrPhone { get; set; } = string.Empty;

    /// <summary>
    /// Delivery channel for the verification code. One of: "email", "sms".
    /// Defaults to "email" if omitted so existing callers keep working.
    /// </summary>
    public string? Channel { get; set; }
}

/// <summary>
/// Response returned after a successful forgot-password request. The
/// session id must be submitted back on the verify step along with
/// the 6-digit code the user received.
///
/// Returned even when the underlying email/phone is not on file, so
/// the response shape cannot be used for account enumeration.
/// </summary>
public sealed class ForgotPasswordResponseDto
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public int CodeLength { get; set; }
    public int ResendCooldownSeconds { get; set; }
    public string Channel { get; set; } = "email";

    /// <summary>
    /// Masked destination for UI display (e.g. "j***@gmail.com",
    /// "+27** *** *123"). Safe to show to the user — reveals enough
    /// to identify the account without leaking the full value.
    /// </summary>
    public string DestinationMasked { get; set; } = string.Empty;
}
