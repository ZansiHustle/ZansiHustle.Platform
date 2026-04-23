using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ZansiHustle.Application.Auth;
using ZansiHustle.Application.Auth.Dtos;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers;

/// <summary>
/// Exposes authentication and account-security endpoints.
/// </summary>
[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _config;

    public AuthController(IAuthService authService, ICurrentUserService currentUserService, IConfiguration config)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _config = config;
    }

    // Canonical portal origin for auth email callbacks. The API host is NOT
    // correct here — an email asking a user to reset their password has to
    // link back to the portal (which owns /auth/* routes), not the API.
    //
    // Resolution order:
    //   1. Auth:PortalBaseUrl         (preferred, auth-specific override)
    //   2. Referrals:PortalBaseUrl    (re-used existing portal config)
    //   3. https://portal.zansihustle.co.za  (prod default, fail-safe)
    private string GetPortalBaseUrl()
    {
        var host = _config["Auth:PortalBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(host))
            host = _config["Referrals:PortalBaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(host))
            host = "https://portal.zansihustle.co.za";
        return host;
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthTokenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return ToActionResult(result);
    }

    /// <summary>
    /// Registers a new ZansiHustle user account.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return ToActionResult(result);
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<AuthTokenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Sends an email verification link to a user.
    /// </summary>
    [HttpPost("verify-email/send")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendVerificationEmail([FromBody] SendEmailVerificationRequestDto dto)
    {
        // Callback must point at the PORTAL (not the API host), and the
        // path must match the portal route: /auth/verify-email.
        var callback = $"{GetPortalBaseUrl()}/auth/verify-email";
        var result = await _authService.SendEmailVerificationAsync(dto.Email, callback);
        return ToActionResult(result);
    }

    /// <summary>
    /// Confirms a user's email address using a token.
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto dto)
    {
        var result = await _authService.VerifyEmailAsync(dto.UserId, dto.Token);
        return ToActionResult(result);
    }

    /// <summary>
    /// Starts the forgot-password flow.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        // Callback must point at the PORTAL (not the API host), and the
        // path must match the portal route: /auth/reset-password. The old
        // callback ("{api-host}/reset-password") produced 404 email links.
        var callback = $"{GetPortalBaseUrl()}/auth/reset-password";
        var result = await _authService.ForgotPasswordAsync(dto.Email, callback);
        return ToActionResult(result);
    }

    /// <summary>
    /// Resets a password using a reset token.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        var result = await _authService.ResetPasswordAsync(dto.UserId, dto.Token, dto.NewPassword);
        return ToActionResult(result);
    }

    /// <summary>
    /// Changes the current user's password.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequestDto dto)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                code = "UNAUTHORIZED",
                message = "User identifier not found in token."
            });
        }

        var result = await _authService.ChangePasswordAsync(_currentUserService.UserId.Value, dto.CurrentPassword, dto.NewPassword);

        return ToActionResult(result);
    }

    /// <summary>
    /// Returns information about the current authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(Result<CurrentUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                code = "UNAUTHORIZED",
                message = "User identifier not found in token."
            });
        }

        var result = await _authService.GetCurrentUserAsync(_currentUserService.UserId.Value);
        return ToActionResult(result);
    }

    /// <summary>
    /// Revokes the current user's active refresh tokens.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return Unauthorized(new
            {
                success = false,
                code = "UNAUTHORIZED",
                message = "User identifier not found in token."
            });
        }

        var result = await _authService.LogoutAsync(_currentUserService.UserId.Value);
        return ToActionResult(result);
    }
}