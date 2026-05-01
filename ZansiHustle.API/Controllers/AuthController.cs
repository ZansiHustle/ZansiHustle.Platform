using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ZansiHustle.Application.Auth;
using ZansiHustle.Application.Auth.Dtos;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Communications.PhoneVerification;
using ZansiHustle.Shared.Errors;
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
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly IConfiguration _config;

    public AuthController(
        IAuthService authService,
        ICurrentUserService currentUserService,
        IPhoneVerificationService phoneVerificationService,
        IConfiguration config)
    {
        _authService = authService;
        _currentUserService = currentUserService;
        _phoneVerificationService = phoneVerificationService;
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
    /// Starts the forgot-password flow by issuing a 6-digit OTP on the
    /// requested channel ("email" or "sms"). Returns a session id the
    /// client presents on <c>/verify-reset-otp</c>.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<ForgotPasswordResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        if (dto is null)
            return ToActionResult(Result<ForgotPasswordResponseDto>.Failure(
                ErrorCodes.BadRequest, "Request is required."));
        var result = await _authService.RequestPasswordResetOtpAsync(dto);
        return ToActionResult(result);
    }

    /// <summary>
    /// Verifies the 6-digit OTP issued by <c>/forgot-password</c>. On
    /// success returns { userId, resetToken } — the caller passes both
    /// into <c>/reset-password</c> to finalise.
    /// </summary>
    [HttpPost("verify-reset-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<VerifyResetOtpResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyResetOtp([FromBody] VerifyResetOtpRequestDto dto)
    {
        if (dto is null)
            return ToActionResult(Result<VerifyResetOtpResponseDto>.Failure(
                ErrorCodes.BadRequest, "Request is required."));
        var result = await _authService.VerifyPasswordResetOtpAsync(dto);
        return ToActionResult(result);
    }

    /// <summary>
    /// Resets a password using the Identity token minted by
    /// <c>/verify-reset-otp</c>.
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

    /// <summary>
    /// Sends a one-time verification code via Twilio Verify. Accepts SA
    /// phone numbers in any of these formats: <c>0791234567</c>,
    /// <c>27791234567</c>, or already-E.164 <c>+27791234567</c>. The server
    /// normalizes before dispatching.
    ///
    /// <para>
    /// <b>Channel</b> defaults to <c>"sms"</c> when omitted; pass
    /// <c>"whatsapp"</c> to dispatch over WhatsApp instead. A short
    /// per-(phone, channel) cooldown is enforced — an SMS cooldown does
    /// not block a WhatsApp fallback. Clients hitting it receive
    /// <c>OTP_RESEND_COOLDOWN</c>.
    /// </para>
    /// </summary>
    [HttpPost("send-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<SendOtpResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto is null)
            return ToActionResult(Result<SendOtpResult>.Failure(
                ErrorCodes.BadRequest, "Request is required."));

        if (!MobileOtpChannels.TryParse(dto.Channel, out var channel))
            return ToActionResult(Result<SendOtpResult>.Failure(
                ErrorCodes.BadRequest, "Unsupported OTP channel. Allowed: sms, whatsapp."));

        var result = await _phoneVerificationService.SendOtpAsync(dto.PhoneNumber, channel, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Checks an SMS code issued by <c>/send-otp</c> against Twilio Verify.
    /// Returns the normalized phone number on success so the client can use
    /// it to drive subsequent auth steps (sign-up, login, phone-confirm).
    /// </summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<VerifyOtpResult>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequestDto dto, CancellationToken cancellationToken)
    {
        if (dto is null)
            return ToActionResult(Result<VerifyOtpResult>.Failure(
                ErrorCodes.BadRequest, "Request is required."));

        var result = await _phoneVerificationService.VerifyOtpAsync(dto.PhoneNumber, dto.Code, cancellationToken);
        return ToActionResult(result);
    }
}