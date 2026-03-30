using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Dashboard.Dtos;
using ZansiHustle.Application.Persistence.Dashboard;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Dashboard
{
    /// <summary>
    /// Provides business logic for launch operations dashboard queries.
    /// </summary>
    public class LaunchOpsDashboardService : ILaunchOpsDashboardService
    {
        private readonly ILaunchOpsDashboardRepository _launchOpsDashboardRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="LaunchOpsDashboardService"/> class.
        /// </summary>
        public LaunchOpsDashboardService(ILaunchOpsDashboardRepository launchOpsDashboardRepository)
        {
            _launchOpsDashboardRepository = launchOpsDashboardRepository;
        }

        /// <inheritdoc />
        public async Task<Result<LaunchOpsSummaryDto>> GetSummaryAsync()
        {
            try
            {
                var data = await _launchOpsDashboardRepository.GetSummaryAsync();
                return Result<LaunchOpsSummaryDto>.Success(data, "Launch operations summary retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<LaunchOpsSummaryDto>.Failure($"An error occurred while retrieving the launch operations summary. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<ProvinceDistributionDto>>> GetSellerLeadProvinceDistributionAsync()
        {
            try
            {
                var data = await _launchOpsDashboardRepository.GetSellerLeadProvinceDistributionAsync();
                return Result<List<ProvinceDistributionDto>>.Success(data, "Seller lead province distribution retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<ProvinceDistributionDto>>.Failure($"An error occurred while retrieving seller lead province distribution. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<TeamActivityDto>>> GetTeamActivityAsync()
        {
            try
            {
                var data = await _launchOpsDashboardRepository.GetTeamActivityAsync();
                return Result<List<TeamActivityDto>>.Success(data, "Team activity retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<TeamActivityDto>>.Failure($"An error occurred while retrieving team activity. {ex.Message}");
            }
        }
    }
}
