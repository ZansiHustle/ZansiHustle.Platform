namespace ZansiHustle.Shared.Enums.Communications;

/// <summary>
/// Delivery channel for a phone-based OTP issued via a verification provider
/// (Twilio Verify). Intentionally narrower than <see cref="OtpChannel"/>:
/// Email lives on a different code path (custom OtpService) and must not be
/// reachable from the phone-verification surface.
/// </summary>
public enum MobileOtpChannel
{
    Sms = 1,
    WhatsApp = 2
}
