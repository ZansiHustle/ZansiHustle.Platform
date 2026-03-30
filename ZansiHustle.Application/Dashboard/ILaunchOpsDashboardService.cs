using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Dashboard
{
    /// <summary>
    /// Service contract for launch operations dashboard logic.
    /// </summary>
    public interface ILaunchOpsDashboardService
    {
        Task<Result<LaunchOpsSummaryDto>> GetSummaryAsync();
        Task<Result<List<ProvinceDistributionDto>>> GetSellerLeadProvinceDistributionAsync();
        Task<Result<List<TeamActivityDto>>> GetTeamActivityAsync();
    }
}
