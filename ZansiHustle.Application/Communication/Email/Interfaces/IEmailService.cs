using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Email.Interfaces;

/// <summary>
/// Defines application-level email operations for ZansiHustle.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification message to a user.
    /// </summary>
    Task<Result> SendVerifyEmailAsync(string toEmail, string firstName, string verificationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a confirmation email after a user's email address has been verified.
    /// </summary>
    Task<Result> SendEmailVerifiedConfirmationAsync(string toEmail, string firstName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a generic one-time-password email (non-purpose-specific copy).
    /// </summary>
    Task<Result> SendOtpAsync(string toEmail, string firstName, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password-reset OTP email with purpose-specific copy
    /// (greeting, expiry line, "didn't request this?" warning).
    /// </summary>
    Task<Result> SendPasswordResetOtpAsync(string toEmail, string firstName, string otpCode, int ttlMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an ACCOUNT-VERIFICATION OTP email — copy makes clear the code is
    /// for verifying the user's ZansiHustle account (not a password reset).
    /// </summary>
    Task<Result> SendAccountVerificationOtpAsync(string toEmail, string firstName, string otpCode, int ttlMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a forgot-password email containing a reset link. Retained for
    /// any legacy link-based caller; the current product flow uses OTP
    /// via <see cref="SendPasswordResetOtpAsync"/>.
    /// </summary>
    Task<Result> SendPasswordResetAsync(string toEmail, string firstName, string resetLink, CancellationToken cancellationToken = default);
}