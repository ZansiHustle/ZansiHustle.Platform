using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Customers.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Persistence.Admin.Customers
{
    /// <summary>
    /// Admin Customers page queries. Customers are derived from the Orders
    /// table — expand the surface only as the UI does.
    /// </summary>
    public interface IAdminCustomerRepository
    {
        Task<CustomersKpisDto> GetKpisAsync();
        Task<PagedResult<AdminCustomerListItemDto>> GetPagedAsync(PagedListQueryBase query);
    }
}
