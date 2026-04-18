using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Affiliates.Dtos;
using ZansiHustle.Application.Persistence.Admin.Affiliates;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Affiliates
{
    public class AdminAffiliateService : IAdminAffiliateService
    {
        private readonly IAdminAffiliateRepository _repository;

        public AdminAffiliateService(IAdminAffiliateRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<AffiliatesKpisDto>> GetKpisAsync()
        {
            try
            {
                var data = await _repository.GetKpisAsync();
                return Result<AffiliatesKpisDto>.Success(data, "Affiliate KPIs retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<AffiliatesKpisDto>.Failure($"Failed to retrieve affiliate KPIs. {ex.Message}");
            }
        }

        public async Task<Result<PagedResult<AdminAffiliateListItemDto>>> GetAffiliatesAsync(PagedListQueryBase query)
        {
            var safe = PagedQueryGuard.Clamp(query);

            try
            {
                var data = await _repository.GetPagedAsync(safe);
                return Result<PagedResult<AdminAffiliateListItemDto>>.Success(data, "Affiliates retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PagedResult<AdminAffiliateListItemDto>>.Failure($"Failed to retrieve affiliates. {ex.Message}");
            }
        }

        public async Task<Result<List<AffiliatePerformancePointDto>>> GetPerformanceTrendAsync(int months = 7)
        {
            if (months < 1) months = 1;
            if (months > 24) months = 24;

            try
            {
                var data = await _repository.GetPerformanceTrendAsync(months);
                return Result<List<AffiliatePerformancePointDto>>.Success(data, "Affiliate performance trend retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<AffiliatePerformancePointDto>>.Failure($"Failed to retrieve affiliate performance trend. {ex.Message}");
            }
        }
    }
}
