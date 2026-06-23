using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.AppConfigs.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.AppConfigs
{
    /// <summary>
    /// Read/update operations for admin-controlled remote app configs.
    /// </summary>
    public interface IAppRuntimeConfigService
    {
        /// <summary>Admin: all active config rows for the Portal.</summary>
        Task<Result<List<AppConfigAdminDto>>> GetAllAsync();

        /// <summary>Admin: update one config row by key.</summary>
        Task<Result<AppConfigAdminDto>> UpdateAsync(string key, UpdateAppConfigRequestDto request, Guid? updatedByUserId);

        /// <summary>Public: key → enabled/title/message map for the mobile app.</summary>
        Task<Result<PublicAppConfigsResponseDto>> GetPublicAsync();
    }
}
