using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Analytics.Dtos;
using ZansiHustle.Application.Persistence.Analytics;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Analytics
{
    /// <summary>
    /// Analytics business logic. Every method funnels exceptions through a
    /// <see cref="Result{T}"/> failure so the controller never leaks stack
    /// traces.
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _repository;

        public AnalyticsService(IAnalyticsRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<AnalyticsKpisDto>> GetKpisAsync()
        {
            try
            {
                var data = await _repository.GetKpisAsync();
                return Result<AnalyticsKpisDto>.Success(data, "Analytics KPIs retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<AnalyticsKpisDto>.Failure($"Failed to retrieve analytics KPIs. {ex.Message}");
            }
        }

        public async Task<Result<List<RevenuePointDto>>> GetRevenueTrendAsync(int months = 7)
        {
            // Clamp to a sensible range so a bad query string can't cause a
            // runaway aggregation. 1..24 months is plenty for the UI today.
            if (months < 1) months = 1;
            if (months > 24) months = 24;

            try
            {
                var data = await _repository.GetRevenueTrendAsync(months);
                return Result<List<RevenuePointDto>>.Success(data, "Revenue trend retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<RevenuePointDto>>.Failure($"Failed to retrieve revenue trend. {ex.Message}");
            }
        }

        public async Task<Result<List<RegionBreakdownDto>>> GetRegionBreakdownAsync()
        {
            try
            {
                var data = await _repository.GetRegionBreakdownAsync();
                return Result<List<RegionBreakdownDto>>.Success(data, "Regional breakdown retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<RegionBreakdownDto>>.Failure($"Failed to retrieve regional breakdown. {ex.Message}");
            }
        }

        public async Task<Result<List<CategoryBreakdownDto>>> GetCategoryBreakdownAsync()
        {
            try
            {
                var data = await _repository.GetCategoryBreakdownAsync();
                return Result<List<CategoryBreakdownDto>>.Success(data, "Category breakdown retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<CategoryBreakdownDto>>.Failure($"Failed to retrieve category breakdown. {ex.Message}");
            }
        }

        public async Task<Result<AnalyticsOverviewDto>> GetOverviewAsync(int growthMonths = 6)
        {
            if (growthMonths < 1) growthMonths = 1;
            if (growthMonths > 24) growthMonths = 24;

            try
            {
                var data = await _repository.GetOverviewAsync(growthMonths);
                return Result<AnalyticsOverviewDto>.Success(data, "Analytics overview retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<AnalyticsOverviewDto>.Failure($"Failed to retrieve analytics overview. {ex.Message}");
            }
        }
    }
}
