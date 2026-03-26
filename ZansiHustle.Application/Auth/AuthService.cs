using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Auth.Dtos;
using ZansiHustle.Application.Common.Interfaces;
using ZansiHustle.Application.Persistence.Identity;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Auth;

/// <summary>
/// Handles user authentication, account registration, token refresh, email verification,
/// password recovery, and logout operations.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenDto>> LoginAsync(LoginDto dto)
    {
        try
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user == null || !user.IsActive || user.AccountStatus != AccountStatus.Active)
            {
                return Result<AuthTokenDto>.Failure(ErrorCodes.Unauthorized, "Invalid credentials.");
            }

            var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!validPassword)
            {
                return Result<AuthTokenDto>.Failure(ErrorCodes.Unauthorized, "Invalid credentials.");
            }

            if (!user.EmailConfirmed)
            {
                return Result<AuthTokenDto>.Failure(ErrorCodes.Forbidden, "Please verify your email address before logging in.");
            }

            var token = await _jwtTokenGenerator.GenerateTokenAsync(user);

            _logger.LogInformation("User {UserId} logged in successfully.", user.Id);
            return Result<AuthTokenDto>.Success(token, "Login successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for email {Email}.", dto.Email);
            return Result<AuthTokenDto>.Failure(ErrorCodes.Exception, "Login failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> RegisterAsync(RegisterDto dto)
    {
        try
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                return Result<Guid>.Failure(ErrorCodes.Conflict, "Email address is already registered.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                IsActive = true,
                AccountStatus = AccountStatus.Active,
                CreatedOnUtc = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                return Result<Guid>.Failure(ErrorCodes.BadRequest, $"Registration failed: {errors}");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, UserRole.Customer.ToString());
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                return Result<Guid>.Failure(ErrorCodes.BadRequest, $"Role assignment failed: {errors}");
            }

            _logger.LogInformation("User registered successfully. UserId: {UserId}, Email: {Email}", user.Id, user.Email);
            return Result<Guid>.Success(user.Id, "Registration successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email {Email}.", dto.Email);
            return Result<Guid>.Failure(ErrorCodes.Exception, "Registration failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<AuthTokenDto>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokens = await _jwtTokenGenerator.RefreshTokenAsync(refreshToken);
            if (tokens == null)
            {
                return Result<AuthTokenDto>.Failure(ErrorCodes.Unauthorized, "Invalid or expired refresh token.");
            }

            return Result<AuthTokenDto>.Success(tokens, "Token refreshed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token flow failed.");
            return Result<AuthTokenDto>.Failure(ErrorCodes.Exception, "Failed to refresh token.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> SendEmailVerificationAsync(string email, string callbackBaseUrl)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
            if (user == null)
            {
                return Result.Success("If the account exists, a verification email has been sent.");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = $"{callbackBaseUrl}?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            var body = $@"
                <h2>Verify your email</h2>
                <p>Welcome to ZansiHustle.</p>
                <p>Please verify your email by clicking the link below:</p>
                <p><a href=""{callbackUrl}"">Verify Email</a></p>";

            await _emailService.SendAsync(user.Email!, "Verify your ZansiHustle email", body);

            _logger.LogInformation("Email verification link sent to {Email}.", email);
            return Result.Success("Verification email sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}.", email);
            return Result.Failure(ErrorCodes.Exception, "Failed to send verification email.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(string userId, string token)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure(ErrorCodes.NotFound, "User not found.");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                return Result.Failure(ErrorCodes.BadRequest, $"Email verification failed: {errors}");
            }

            return Result.Success("Email verified successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Email verification failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ForgotPasswordAsync(string email, string callbackBaseUrl)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant());
            if (user == null)
            {
                return Result.Success("If the account exists, a password reset email has been sent.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = $"{callbackBaseUrl}?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            var body = $@"
                <h2>Reset your password</h2>
                <p>Click the link below to reset your password:</p>
                <p><a href=""{callbackUrl}"">Reset Password</a></p>";

            await _emailService.SendAsync(user.Email!, "Reset your ZansiHustle password", body);

            _logger.LogInformation("Password reset email sent to {Email}.", email);
            return Result.Success("Password reset email sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}.", email);
            return Result.Failure(ErrorCodes.Exception, "Failed to send password reset email.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure(ErrorCodes.NotFound, "User not found.");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                return Result.Failure(ErrorCodes.BadRequest, $"Password reset failed: {errors}");
            }

            await _jwtTokenGenerator.RevokeAllRefreshTokensForUserAsync(user.Id);

            return Result.Success("Password reset successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed for user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Password reset failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Result.Failure(ErrorCodes.NotFound, "User not found.");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => x.Description));
                return Result.Failure(ErrorCodes.BadRequest, $"Password change failed: {errors}");
            }

            await _jwtTokenGenerator.RevokeAllRefreshTokensForUserAsync(user.Id);

            return Result.Success("Password changed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password change failed for user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Password change failed.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || !user.IsActive || user.AccountStatus != AccountStatus.Active)
            {
                return Result<CurrentUserDto>.Failure(ErrorCodes.NotFound, "User not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var dto = new CurrentUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                AccountStatus = user.AccountStatus.ToString()
            };

            return Result<CurrentUserDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current user {UserId}.", userId);
            return Result<CurrentUserDto>.Failure(ErrorCodes.Exception, "Failed to load current user.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> LogoutAsync(Guid userId)
    {
        try
        {
            await _jwtTokenGenerator.RevokeAllRefreshTokensForUserAsync(userId);
            return Result.Success("Logged out successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout failed for user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Logout failed.");
        }
    }
}