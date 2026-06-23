using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.AppConfigs;

namespace ZansiHustle.Application.Persistence.AppConfigs
{
    /// <summary>
    /// Repository contract for admin-controlled remote app configs.
    /// </summary>
    public interface IAppRuntimeConfigRepository
    {
        /// <summary>All active config rows, ordered for display.</summary>
        Task<List<AppRuntimeConfig>> GetAllAsync();

        /// <summary>Active config rows the public/mobile app may read.</summary>
        Task<List<AppRuntimeConfig>> GetPublicAsync();

        Task<AppRuntimeConfig?> GetByKeyAsync(string key);

        Task AddAsync(AppRuntimeConfig config);
        void Update(AppRuntimeConfig config);

        Task<bool> SaveChangesAsync();
    }
}
