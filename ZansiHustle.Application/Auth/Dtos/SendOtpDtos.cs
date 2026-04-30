namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Request body for <c>POST /api/auth/send-otp</c>. The phone number does not
/// need to be E.164 — the server normalizes SA inputs (0XXXXXXXXX, 27..., +27...)
/// before dispatching to Twilio Verify.
/// </summary>
public sealed class SendOtpRequestDto
{
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// Request body for <c>POST /api/auth/verify-otp</c>. Send back the same phone
/// number the client posted to <c>send-otp</c>; the server re-normalizes it,
/// so any of the accepted local formats works.
/// </summary>
public sealed class VerifyOtpRequestDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
