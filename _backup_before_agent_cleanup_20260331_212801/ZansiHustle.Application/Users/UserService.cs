using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Users;

/// <summary>
/// Handles core user account operations.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<User>> GetByIdAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result<User>.Failure(ErrorCodes.NotFound, "User not found.");

            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user {UserId}.", userId);
            return Result<User>.Failure(ErrorCodes.Exception, "Failed to load user.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<List<User>>> GetAllAsync()
    {
        try
        {
            var users = await _userRepository.GetAllAsync();
            return Result<List<User>>.Success(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load users.");
            return Result<List<User>>.Failure(ErrorCodes.Exception, "Failed to load users.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpdateAsync(Guid userId, string firstName, string lastName, string? phoneNumber)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result.Failure(ErrorCodes.NotFound, "User not found.");

            user.FirstName = firstName.Trim();
            user.LastName = lastName.Trim();
            user.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
            user.UpdatedOnUtc = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return Result.Success("User updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Failed to update user.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(Guid userId)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return Result.Failure(ErrorCodes.NotFound, "User not found.");

            user.IsActive = false;
            user.AccountStatus = AccountStatus.Inactive;
            user.UpdatedOnUtc = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);

            return Result.Success("User deactivated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate user {UserId}.", userId);
            return Result.Failure(ErrorCodes.Exception, "Failed to deactivate user.");
        }
    }
}