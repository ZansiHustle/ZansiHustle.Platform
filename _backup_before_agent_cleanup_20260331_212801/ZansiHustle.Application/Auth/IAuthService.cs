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
    /// Starts the forgot-password flow.
    /// </summary>
    Task<Result> ForgotPasswordAsync(string email, string callbackBaseUrl);

    /// <summary>
    /// Resets a password using a token.
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