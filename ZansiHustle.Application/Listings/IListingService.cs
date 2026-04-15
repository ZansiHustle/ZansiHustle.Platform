using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Listings
{
    /// <summary>
    /// Service contract for listing (product + service) operations.
    /// Write operations enforce shop ownership via the caller's user id.
    /// </summary>
    public interface IListingService
    {
        Task<Result<PagedResult<ListingListItemDto>>> SearchAsync(ListingFilterRequestDto filter);
        Task<Result<ListingDto>> GetByIdAsync(Guid id);
        Task<Result<List<ListingListItemDto>>> GetByMerchantAsync(Guid merchantId);
        Task<Result<List<ListingListItemDto>>> GetMineAsync(Guid ownerUserId);
        Task<Result<ListingDto>> CreateAsync(Guid ownerUserId, CreateListingRequestDto request);
        Task<Result<ListingDto>> UpdateAsync(Guid ownerUserId, Guid listingId, UpdateListingRequestDto request);
        Task<Result> DeleteAsync(Guid ownerUserId, Guid listingId);
    }
}
