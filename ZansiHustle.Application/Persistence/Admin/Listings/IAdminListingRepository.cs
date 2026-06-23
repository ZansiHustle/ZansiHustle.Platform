using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Listings.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings.Dtos;

namespace ZansiHustle.Application.Persistence.Admin.Listings
{
    /// <summary>
    /// Aggregation + listing queries for the admin Listings page. Unlike the
    /// buyer-facing listing query, these apply NO visibility / status / source /
    /// availability gates — admin sees every listing across all merchants.
    /// </summary>
    public interface IAdminListingRepository
    {
        Task<AdminListingsKpisDto> GetKpisAsync();
        Task<PagedResult<ListingListItemDto>> GetPagedAsync(AdminListingQuery query);
    }
}
