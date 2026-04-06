using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Users;

/// <summary>
/// Repository for core user records.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(AppDbContext dbContext, ILogger<UserRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid userId)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == normalizedEmail);
    }

    /// <inheritdoc />
    public async Task<List<User>> GetAllAsync()
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<User> AddAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(User user)
    {
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid userId)
    {
        return await _dbContext.Users.AnyAsync(x => x.Id == userId);
    }

    /// <inheritdoc />
    public async Task<Guid?> SearchByNameAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var normalizedSearch = searchTerm.Trim().ToLowerInvariant();
        var searchParts = normalizedSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var query = _dbContext.Users.AsNoTracking();

        if (searchParts.Length == 1)
        {
            // Single term - search in FirstName or LastName
            var singleTerm = searchParts[0];
            query = query.Where(u => u.FirstName.ToLower().Contains(singleTerm) ||
                                      u.LastName.ToLower().Contains(singleTerm));
        }
        else
        {
            // Multiple terms - try to match first name and last name
            var firstNameTerm = searchParts[0];
            var lastNameTerm = string.Join(" ", searchParts.Skip(1));

            query = query.Where(u => (u.FirstName.ToLower().Contains(firstNameTerm) &&
                                       u.LastName.ToLower().Contains(lastNameTerm)) ||
                                      u.FirstName.ToLower().Contains(normalizedSearch) ||
                                      u.LastName.ToLower().Contains(normalizedSearch) ||
                                      (u.FirstName + " " + u.LastName).ToLower().Contains(normalizedSearch));
        }

        // Only return active users who could be agents (Marketplace Growth Associates)
        // You can filter by role if you have role information
        var results = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .FirstOrDefaultAsync();

        return results?.Id ?? null;
    }
}