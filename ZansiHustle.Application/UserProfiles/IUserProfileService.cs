using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Users;

/// <summary>
/// Defines operations for user profile management.
/// </summary>
public interface IUserProfileService
{
    /// <summary>
    /// Gets a user's profile by user identifier.
    /// </summary>
    Task<Result<UserProfile>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Creates or updates a user's profile.
    /// </summary>
    Task<Result<UserProfile>> UpsertAsync(Guid userId, string? profileImageUrl, string? bio, string? city, string? province);
}