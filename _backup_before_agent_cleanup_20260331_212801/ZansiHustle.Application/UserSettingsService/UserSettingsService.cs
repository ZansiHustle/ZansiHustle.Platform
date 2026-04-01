using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Users;

/// <summary>
/// Handles user settings operations.
/// </summary>
public class UserSettingsService : IUserSettingsService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly ILogger<UserSettingsService> _logger;

    public UserSettingsService(
        IUserRepository userRepository,
        IUserSettingsRepository userSettingsRepository,
        ILogger<UserSettingsService> logger)
    {
        _userRepository = userRepository;
        _userSettingsRepository = userSettingsRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<UserSettings>> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var settings = await _userSettingsRepository.GetByUserIdAsync(userId);
            if (settings == null)
                return Result<UserSettings>.Failure(ErrorCodes.NotFound, "User settings not found.");

            return Result<UserSettings>.Success(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings for user {UserId}.", userId);
            return Result<UserSettings>.Failure(ErrorCodes.Exception, "Failed to load user settings.");
        }
    }

    /// <inheritdoc />
    public async Task<Result<UserSettings>> UpsertAsync(Guid userId, bool emailNotificationsEnabled, bool pushNotificationsEnabled)
    {
        try
        {
            var userExists = await _userRepository.ExistsAsync(userId);
            if (!userExists)
                return Result<UserSettings>.Failure(ErrorCodes.NotFound, "User not found.");

            var settings = await _userSettingsRepository.GetByUserIdAsync(userId);

            if (settings == null)
            {
                settings = new UserSettings
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    EmailNotificationsEnabled = emailNotificationsEnabled,
                    PushNotificationsEnabled = pushNotificationsEnabled,
                    CreatedOnUtc = DateTime.UtcNow
                };

                await _userSettingsRepository.AddAsync(settings);
            }
            else
            {
                settings.EmailNotificationsEnabled = emailNotificationsEnabled;
                settings.PushNotificationsEnabled = pushNotificationsEnabled;
                settings.UpdatedOnUtc = DateTime.UtcNow;

                await _userSettingsRepository.UpdateAsync(settings);
            }

            return Result<UserSettings>.Success(settings, "User settings saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings for user {UserId}.", userId);
            return Result<UserSettings>.Failure(ErrorCodes.Exception, "Failed to save user settings.");
        }
    }
}