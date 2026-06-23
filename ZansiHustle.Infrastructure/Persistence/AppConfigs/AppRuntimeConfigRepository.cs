using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.AppConfigs;
using ZansiHustle.Domain.AppConfigs;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.AppConfigs
{
    public class AppRuntimeConfigRepository : IAppRuntimeConfigRepository
    {
        private readonly AppDbContext _context;

        public AppRuntimeConfigRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AppRuntimeConfig>> GetAllAsync()
        {
            return await _context.AppRuntimeConfigs
                .Where(x => x.IsActive)
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();
        }

        public async Task<List<AppRuntimeConfig>> GetPublicAsync()
        {
            return await _context.AppRuntimeConfigs
                .AsNoTracking()
                .Where(x => x.IsActive && x.IsPublic)
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToListAsync();
        }

        public async Task<AppRuntimeConfig?> GetByKeyAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var normalized = key.Trim();
            return await _context.AppRuntimeConfigs
                .FirstOrDefaultAsync(x => x.Key == normalized);
        }

        public async Task AddAsync(AppRuntimeConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            await _context.AppRuntimeConfigs.AddAsync(config);
        }

        public void Update(AppRuntimeConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);
            _context.AppRuntimeConfigs.Update(config);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
