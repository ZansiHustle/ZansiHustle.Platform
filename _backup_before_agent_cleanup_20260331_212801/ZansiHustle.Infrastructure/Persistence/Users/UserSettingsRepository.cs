using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Users;

/// <summary>
/// Repository for user settings records.
/// </summary>
public class UserSettingsRepository : IUserSettingsRepository
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserSettingsRepository> _logger;

    public UserSettingsRepository(AppDbContext dbContext, ILogger<UserSettingsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserSettings?> GetByUserIdAsync(Guid userId)
    {
        return await _dbContext.UserSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<UserSettings> AddAsync(UserSettings settings)
    {
        _dbContext.UserSettings.Add(settings);
        await _dbContext.SaveChangesAsync();
        return settings;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UserSettings settings)
    {
        _dbContext.UserSettings.Update(settings);
        await _dbContext.SaveChangesAsync();
    }
}