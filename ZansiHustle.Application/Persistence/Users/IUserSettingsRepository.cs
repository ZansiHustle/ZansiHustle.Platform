using ZansiHustle.Domain.Identity;

namespace ZansiHustle.Application.Persistence.Users;

/// <summary>
/// Defines persistence operations for user settings.
/// </summary>
public interface IUserSettingsRepository
{
    /// <summary>
    /// Gets settings by user identifier.
    /// </summary>
    Task<UserSettings?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Adds new user settings.
    /// </summary>
    Task<UserSettings> AddAsync(UserSettings settings);

    /// <summary>
    /// Updates existing user settings.
    /// </summary>
    Task UpdateAsync(UserSettings settings);
}