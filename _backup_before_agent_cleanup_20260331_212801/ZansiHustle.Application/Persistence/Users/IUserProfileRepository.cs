using ZansiHustle.Domain.Identity;

namespace ZansiHustle.Application.Persistence.Users;

/// <summary>
/// Defines persistence operations for user profiles.
/// </summary>
public interface IUserProfileRepository
{
    /// <summary>
    /// Gets a profile by user identifier.
    /// </summary>
    Task<UserProfile?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Adds a new profile record.
    /// </summary>
    Task<UserProfile> AddAsync(UserProfile profile);

    /// <summary>
    /// Updates an existing profile record.
    /// </summary>
    Task UpdateAsync(UserProfile profile);
}