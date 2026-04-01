using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Dashboard
{
    /// <summary>
    /// Service contract for marketing dashboard logic.
    /// </summary>
    public interface IMarketingDashboardService
    {
        Task<Result<MarketingSummaryDto>> GetMarketingSummaryAsync();
        Task<Result<SocialMediaSummaryDto>> GetSocialMediaSummaryAsync();
        Task<Result<BudgetSummaryDto>> GetBudgetSummaryAsync();
        Task<Result<List<PlatformDistributionDto>>> GetInfluencerPlatformDistributionAsync();
        Task<Result<List<ProvinceDistributionDto>>> GetInfluencerProvinceDistributionAsync();
        Task<Result<List<CampaignPerformanceDto>>> GetCampaignPerformanceAsync();
        Task<Result<List<TrendPointDto>>> GetCampaignTrendsAsync();
    }
}
