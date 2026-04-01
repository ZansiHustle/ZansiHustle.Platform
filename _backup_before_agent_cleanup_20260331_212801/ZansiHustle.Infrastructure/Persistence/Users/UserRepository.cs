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
}