using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Listings.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Listings
{
    /// <summary>
    /// Service contract for the cross-merchant admin Listings views.
    /// </summary>
    public interface IAdminListingService
    {
        Task<Result<AdminListingsKpisDto>> GetKpisAsync();
        Task<Result<PagedResult<ListingListItemDto>>> GetListingsAsync(AdminListingQuery query);
    }
}
