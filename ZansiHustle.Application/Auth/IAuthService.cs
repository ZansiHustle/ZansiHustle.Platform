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
}