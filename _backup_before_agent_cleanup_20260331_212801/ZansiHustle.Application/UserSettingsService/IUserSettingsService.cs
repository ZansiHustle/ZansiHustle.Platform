using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Users;

/// <summary>
/// Defines operations for user settings management.
/// </summary>
public interface IUserSettingsService
{
    /// <summary>
    /// Gets a user's settings by user identifier.
    /// </summary>
    Task<Result<UserSettings>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Creates or updates a user's settings.
    /// </summary>
    Task<Result<UserSettings>> UpsertAsync(Guid userId, bool emailNotificationsEnabled, bool pushNotificationsEnabled);
}