using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Analytics.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Analytics
{
    /// <summary>
    /// Service contract for admin analytics dashboards. Thin wrapper around
    /// <see cref="Persistence.Analytics.IAnalyticsRepository"/> with a
    /// <see cref="Result{T}"/> envelope so failures reach the controller
    /// uniformly.
    /// </summary>
    public interface IAnalyticsService
    {
        Task<Result<AnalyticsKpisDto>> GetKpisAsync();
        Task<Result<List<RevenuePointDto>>> GetRevenueTrendAsync(int months = 7);
        Task<Result<List<RegionBreakdownDto>>> GetRegionBreakdownAsync();
        Task<Result<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync();
    }
}
