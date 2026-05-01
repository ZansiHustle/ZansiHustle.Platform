namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Request body for <c>POST /api/auth/send-otp</c>. The phone number does not
/// need to be E.164 — the server normalizes SA inputs (0XXXXXXXXX, 27..., +27...)
/// before dispatching to Twilio Verify.
///
/// <para>
/// <b>Channel</b> is optional and defaults to SMS when omitted, so older
/// mobile clients that pre-date this field continue to work unchanged.
/// Accepted values: <c>"sms"</c> (default), <c>"whatsapp"</c>. Anything
/// else is rejected with <c>BAD_REQUEST</c>; clients cannot smuggle
/// arbitrary strings through to the verification provider.
/// </para>
/// </summary>
public sealed class SendOtpRequestDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Channel { get; set; }
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
