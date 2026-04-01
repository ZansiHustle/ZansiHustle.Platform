using ZansiHustle.Domain.Identity;

namespace ZansiHustle.Application.Persistence.Users;

/// <summary>
/// Defines persistence operations for core user records.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by unique identifier.
    /// </summary>
    Task<User?> GetByIdAsync(Guid userId);

    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Gets all users.
    /// </summary>
    Task<List<User>> GetAllAsync();

    /// <summary>
    /// Adds a new user record.
    /// </summary>
    Task<User> AddAsync(User user);

    /// <summary>
    /// Updates an existing user record.
    /// </summary>
    Task UpdateAsync(User user);

    /// <summary>
    /// Checks whether a user exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid userId);
}