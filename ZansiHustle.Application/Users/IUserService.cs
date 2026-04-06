using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Users;

/// <summary>
/// Defines core user account operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    Task<Result<User>> GetByIdAsync(Guid userId);

    /// <summary>
    /// Gets all users.
    /// </summary>
    Task<Result<List<User>>> GetAllAsync();

    /// <summary>
    /// Updates core user account details.
    /// </summary>
    Task<Result> UpdateAsync(Guid userId, string firstName, string lastName, string? phoneNumber);

    /// <summary>
    /// Deactivates a user account.
    /// </summary>
    Task<Result> DeactivateAsync(Guid userId);

    /// <summary>
    /// Searches for users by name (first name, last name, or full name).
    /// </summary>
    Task<Result<Guid?>> SearchByNameAsync(string searchTerm);
}