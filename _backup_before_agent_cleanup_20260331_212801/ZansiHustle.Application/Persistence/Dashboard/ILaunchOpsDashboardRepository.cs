using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;

namespace ZansiHustle.Application.Persistence.Dashboard
{
    /// <summary>
    /// Repository contract for launch operations dashboard queries.
    /// </summary>
    public interface ILaunchOpsDashboardRepository
    {
        Task<LaunchOpsSummaryDto> GetSummaryAsync();
        Task<List<ProvinceDistributionDto>> GetSellerLeadProvinceDistributionAsync();
        Task<List<TeamActivityDto>> GetTeamActivityAsync();
    }
}
