using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;

namespace ZansiHustle.Application.Persistence.Dashboard
{
    /// <summary>
    /// Repository contract for marketing dashboard queries.
    /// </summary>
    public interface IMarketingDashboardRepository
    {
        Task<MarketingSummaryDto> GetMarketingSummaryAsync();
        Task<SocialMediaSummaryDto> GetSocialMediaSummaryAsync();
        Task<BudgetSummaryDto> GetBudgetSummaryAsync();
        Task<List<PlatformDistributionDto>> GetInfluencerPlatformDistributionAsync();
        Task<List<ProvinceDistributionDto>> GetInfluencerProvinceDistributionAsync();
        Task<List<CampaignPerformanceDto>> GetCampaignPerformanceAsync();
        Task<List<TrendPointDto>> GetCampaignTrendsAsync();
    }
}
