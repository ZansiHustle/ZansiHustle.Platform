using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.PhoneVerification;

/// <summary>
/// Issues and checks phone-based one-time codes via a verification provider
/// (currently Twilio Verify). The interface is intentionally narrow so we can
/// swap to a South African local sender once Twilio has approved the regulatory
/// bundle — or to an entirely different provider — without touching callers.
/// </summary>
public interface IPhoneVerificationService
{
    /// <summary>
    /// Sends an OTP to the given phone number on the requested channel. The
    /// phone number does NOT need to be E.164 — implementations normalize
    /// South African inputs internally. A short per-(phone,channel) cooldown
    /// is enforced; callers that hit it receive <c>OTP_RESEND_COOLDOWN</c>.
    /// Cooldown is keyed by channel so an SMS cooldown does not block a
    /// WhatsApp fallback in the same flow.
    /// </summary>
    Task<Result<SendOtpResult>> SendOtpAsync(
        string phoneNumber,
        MobileOtpChannel channel = MobileOtpChannel.Sms,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an OTP previously sent by <see cref="SendOtpAsync"/>. The
    /// provider validates the code against the (service, phone) tuple — it
    /// is channel-independent, so a code sent over SMS can be verified the
    /// same way as one sent over WhatsApp. Anything other than provider
    /// "approved" is mapped to <c>OTP_INVALID</c> so we never leak
    /// provider-specific error strings to the client.
    /// </summary>
    Task<Result<VerifyOtpResult>> VerifyOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken = default);
}

/// <summary>
/// Successful issue payload. <see cref="PhoneNumber"/> is the normalized E.164
/// value the client should round-trip back on verify.
/// </summary>
public sealed class SendOtpResult
{
    public string PhoneNumber { get; set; } = string.Empty;
    public MobileOtpChannel Channel { get; set; } = MobileOtpChannel.Sms;
    public int ResendCooldownSeconds { get; set; }
}

/// <summary>
/// Successful verification payload. The caller decides what to do with this
/// (mark phone confirmed on a User, gate auth flow, etc.) — this service does
/// not persist anything itself.
/// </summary>
public sealed class VerifyOtpResult
{
    public string PhoneNumber { get; set; } = string.Empty;
    public bool Verified { get; set; }
}
