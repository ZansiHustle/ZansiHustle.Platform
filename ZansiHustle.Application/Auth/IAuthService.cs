using ZansiHustle.Application.Auth.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Auth;

/// <summary>
/// Defines authentication and account security operations for ZansiHustle users.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user using email and password.
    /// </summary>
    Task<Result<AuthTokenDto>> LoginAsync(LoginDto dto);

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    Task<Result<Guid>> RegisterAsync(RegisterDto dto);

    /// <summary>
    /// Checks whether an email and/or phone number are still available
    /// for registration. Pure read — no account is created or modified.
    /// Used by the registration wizard's Step&nbsp;1 ("Contact") so the
    /// user is told about a duplicate before they fill in the rest of
    /// the form.
    /// </summary>
    Task<Result<CheckAvailabilityResponseDto>> CheckAvailabilityAsync(CheckAvailabilityRequestDto dto);

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    Task<Result<AuthTokenDto>> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Sends an email verification link to a user.
    /// </summary>
    Task<Result> SendEmailVerificationAsync(string email, string callbackBaseUrl);

    /// <summary>
    /// Verifies a user's email address using a token.
    /// </summary>
    Task<Result> VerifyEmailAsync(string userId, string token);

    /// <summary>
    /// Legacy link-based forgot-password flow — retained for any caller
    /// that still wants an email with a reset link. The current product
    /// flow uses <see cref="RequestPasswordResetOtpAsync"/> instead.
    /// </summary>
    Task<Result> ForgotPasswordAsync(string email, string callbackBaseUrl);

    /// <summary>
    /// Current forgot-password entry point. Issues a 6-digit OTP on the
    /// requested channel ("email" or "sms") and returns a session id
    /// the client must present on the verify step. Enumeration-safe:
    /// the response shape is identical whether or not the identifier
    /// exists in the system.
    /// </summary>
    Task<Result<ForgotPasswordResponseDto>> RequestPasswordResetOtpAsync(ForgotPasswordRequestDto dto);

    /// <summary>
    /// Verifies the submitted OTP against the session issued by
    /// <see cref="RequestPasswordResetOtpAsync"/>. On success, mints an
    /// Identity password-reset token and returns it together with the
    /// user id. The client then calls <see cref="ResetPasswordAsync"/>
    /// to finalise the reset.
    /// </summary>
    Task<Result<VerifyResetOtpResponseDto>> VerifyPasswordResetOtpAsync(VerifyResetOtpRequestDto dto);

    /// <summary>
    /// Resets a password using the Identity token minted by
    /// <see cref="VerifyPasswordResetOtpAsync"/>.
    /// </summary>
    Task<Result> ResetPasswordAsync(string userId, string token, string newPassword);

    /// <summary>
    /// Changes the password for an authenticated user.
    /// </summary>
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    /// <summary>
    /// Gets the current authenticated user's information.
    /// </summary>
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId);

    /// <summary>
    /// Logs a user out by revoking their active refresh tokens.
    /// </summary>
    Task<Result> LogoutAsync(Guid userId);

    /// <summary>
    /// Issues an ACCOUNT-VERIFICATION OTP to the user's email (purpose
    /// EmailVerification, distinct from password reset). Enumeration-safe — the
    /// response shape is identical whether or not the email is on file.
    /// </summary>
    Task<Result<EmailOtpSessionDto>> RequestAccountEmailOtpAsync(string email);

    /// <summary>
    /// Verifies an account-verification email OTP. On success sets
    /// <c>EmailConfirmed = true</c> for the session's user (never the wrong
    /// account — the user id is bound to the OTP session) and returns verified.
    /// </summary>
    Task<Result<VerifyEmailOtpResponseDto>> VerifyAccountEmailOtpAsync(string sessionId, string code);

    /// <summary>
    /// Persists phone verification (sets <c>PhoneNumberConfirmed = true</c>) for
    /// the user that owns the given phone number, after a successful OTP check.
    /// Matches on E.164 / local / raw forms and only updates when EXACTLY one
    /// account matches (never confirms the wrong user). Best-effort — never
    /// throws, so it can't break the verify-otp response.
    /// </summary>
    Task MarkPhoneConfirmedAsync(string rawPhone, string? normalizedPhone);
}