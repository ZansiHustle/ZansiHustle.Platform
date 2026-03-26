using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Users;

/// <summary>
/// Repository for user profile records.
/// </summary>
public class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserProfileRepository> _logger;

    public UserProfileRepository(AppDbContext dbContext, ILogger<UserProfileRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<UserProfile> AddAsync(UserProfile profile)
    {
        _dbContext.UserProfiles.Add(profile);
        await _dbContext.SaveChangesAsync();
        return profile;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UserProfile profile)
    {
        _dbContext.UserProfiles.Update(profile);
        await _dbContext.SaveChangesAsync();
    }
}