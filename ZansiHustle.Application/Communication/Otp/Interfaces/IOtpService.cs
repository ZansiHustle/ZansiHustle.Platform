using ZansiHustle.Application.Communications.Otp.Models;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Otp.Interfaces;

/// <summary>
/// Issues and verifies one-time passwords across SMS, WhatsApp, and email channels.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Issues a new OTP for the given destination/purpose and dispatches it on
    /// the requested channel. Returns a session id the caller must present on verify.
    /// </summary>
    Task<Result<OtpIssueResult>> IssueAsync(OtpIssueRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a submitted code against an issued session. On success the
    /// session is consumed and cannot be reused.
    /// </summary>
    Task<Result<OtpVerifiedContext>> VerifyAsync(OtpVerifyRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context returned on successful verification — business flows use this to
/// decide what to do next (activate account, mark phone verified, etc.).
/// </summary>
public sealed class OtpVerifiedContext
{
    public string Destination { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public ZansiHustle.Shared.Enums.Communications.OtpPurpose Purpose { get; set; }
    public ZansiHustle.Shared.Enums.Communications.OtpChannel Channel { get; set; }
}
