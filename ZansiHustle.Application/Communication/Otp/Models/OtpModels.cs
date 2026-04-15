using ZansiHustle.Shared.Enums.Communications;

namespace ZansiHustle.Application.Communications.Otp.Models;

/// <summary>
/// Request to issue a new OTP code.
/// </summary>
public sealed class OtpIssueRequest
{
    /// <summary>
    /// The destination to send the code to. For SMS/WhatsApp this is an
    /// E.164 phone number; for Email this is an email address.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Which channel to deliver the code on.
    /// </summary>
    public OtpChannel Channel { get; set; } = OtpChannel.Sms;

    /// <summary>
    /// Business purpose of the OTP.
    /// </summary>
    public OtpPurpose Purpose { get; set; } = OtpPurpose.PhoneVerification;

    /// <summary>
    /// Optional user id if the OTP is tied to a specific user.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Optional display name for personalised messages.
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// Result of a successful OTP issuance.
/// </summary>
public sealed class OtpIssueResult
{
    /// <summary>Opaque session id clients must present when verifying.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>UTC timestamp at which the issued code expires.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Number of digits in the issued code (for client display hints).</summary>
    public int CodeLength { get; set; }

    /// <summary>Seconds the caller must wait before requesting a resend for the same destination/purpose.</summary>
    public int ResendCooldownSeconds { get; set; }
}

/// <summary>
/// Request to verify a code against an issued session.
/// </summary>
public sealed class OtpVerifyRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
