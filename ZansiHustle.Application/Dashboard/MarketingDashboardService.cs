using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Dashboard
{
    /// <summary>
    /// Provides business logic for marketing dashboard queries.
    /// </summary>
    public class MarketingDashboardService : IMarketingDashboardService
    {
        private readonly IMarketingDashboardRepository _marketingDashboardRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="MarketingDashboardService"/> class.
        /// </summary>
        public MarketingDashboardService(IMarketingDashboardRepository marketingDashboardRepository)
        {
            _marketingDashboardRepository = marketingDashboardRepository;
        }

        /// <inheritdoc />
        public async Task<Result<MarketingSummaryDto>> GetMarketingSummaryAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetMarketingSummaryAsync();
                return Result<MarketingSummaryDto>.Success(data, "Marketing summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<MarketingSummaryDto>.Failure($"An error occurred while retrieving the marketing summary. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<SocialMediaSummaryDto>> GetSocialMediaSummaryAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetSocialMediaSummaryAsync();
                return Result<SocialMediaSummaryDto>.Success(data, "Social media summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<SocialMediaSummaryDto>.Failure($"An error occurred while retrieving the social media summary. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BudgetSummaryDto>> GetBudgetSummaryAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetBudgetSummaryAsync();
                return Result<BudgetSummaryDto>.Success(data, "Budget summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<BudgetSummaryDto>.Failure($"An error occurred while retrieving the budget summary. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<PlatformDistributionDto>>> GetInfluencerPlatformDistributionAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetInfluencerPlatformDistributionAsync();
                return Result<List<PlatformDistributionDto>>.Success(data, "Influencer platform distribution retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<PlatformDistributionDto>>.Failure($"An error occurred while retrieving influencer platform distribution. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ProvinceDistributionDto>>> GetInfluencerProvinceDistributionAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetInfluencerProvinceDistributionAsync();
                return Result<List<ProvinceDistributionDto>>.Success(data, "Influencer province distribution retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<ProvinceDistributionDto>>.Failure($"An error occurred while retrieving influencer province distribution. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<CampaignPerformanceDto>>> GetCampaignPerformanceAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetCampaignPerformanceAsync();
                return Result<List<CampaignPerformanceDto>>.Success(data, "Campaign performance retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<CampaignPerformanceDto>>.Failure($"An error occurred while retrieving campaign performance. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<TrendPointDto>>> GetCampaignTrendsAsync()
        {
            try
            {
                var data = await _marketingDashboardRepository.GetCampaignTrendsAsync();
                return Result<List<TrendPointDto>>.Success(data, "Campaign trends retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<TrendPointDto>>.Failure($"An error occurred while retrieving campaign trends. {ex.Message}");
            }
        }
    }
}
