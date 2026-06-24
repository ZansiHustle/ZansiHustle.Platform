using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Analytics.Dtos;

namespace ZansiHustle.Application.Persistence.Analytics
{
    /// <summary>
    /// Aggregation queries backing the admin analytics endpoints. Intentionally
    /// narrow: one method per rendered chart so callers stay decoupled from EF.
    /// </summary>
    public interface IAnalyticsRepository
    {
        Task<AnalyticsKpisDto> GetKpisAsync();
        Task<List<RevenuePointDto>> GetRevenueTrendAsync(int months);
        Task<List<RegionBreakdownDto>> GetRegionBreakdownAsync();
        Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync();

        /// <summary>Live command-center snapshot (summary + growth + sections).</summary>
        Task<AnalyticsOverviewDto> GetOverviewAsync(int growthMonths);
    }
}
