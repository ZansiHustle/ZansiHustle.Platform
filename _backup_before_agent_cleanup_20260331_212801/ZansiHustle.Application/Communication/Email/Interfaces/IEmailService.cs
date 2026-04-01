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
    /// Sends a one-time-password email.
    /// </summary>
    Task<Result> SendOtpAsync(string toEmail, string firstName, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a forgot-password email containing a reset link.
    /// </summary>
    Task<Result> SendPasswordResetAsync(string toEmail, string firstName, string resetLink, CancellationToken cancellationToken = default);
}