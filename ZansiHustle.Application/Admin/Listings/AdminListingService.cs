using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Listings.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Persistence.Admin.Listings;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Listings
{
    /// <summary>
    /// Thin pass-through service. Error handling is centralized here so the
    /// controller never leaks EF exceptions.
    /// </summary>
    public class AdminListingService : IAdminListingService
    {
        private readonly IAdminListingRepository _repository;

        public AdminListingService(IAdminListingRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<AdminListingsKpisDto>> GetKpisAsync()
        {
            try
            {
                var data = await _repository.GetKpisAsync();
                return Result<AdminListingsKpisDto>.Success(data, "Listing KPIs retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<AdminListingsKpisDto>.Failure($"Failed to retrieve listing KPIs. {ex.Message}");
            }
        }

        public async Task<Result<PagedResult<ListingListItemDto>>> GetListingsAsync(AdminListingQuery query)
        {
            // Clamp paging in place (PagedQueryGuard mutates the instance we pass),
            // then keep using the derived query so the Type/Source filters survive.
            query ??= new AdminListingQuery();
            PagedQueryGuard.Clamp(query);

            try
            {
                var data = await _repository.GetPagedAsync(query);
                return Result<PagedResult<ListingListItemDto>>.Success(data, "Listings retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PagedResult<ListingListItemDto>>.Failure($"Failed to retrieve listings. {ex.Message}");
            }
        }
    }
}
