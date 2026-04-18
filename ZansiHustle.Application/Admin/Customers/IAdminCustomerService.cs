using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Customers.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Customers
{
    /// <summary>
    /// Service contract for the admin Customers views.
    /// </summary>
    public interface IAdminCustomerService
    {
        Task<Result<CustomersKpisDto>> GetKpisAsync();
        Task<Result<PagedResult<AdminCustomerListItemDto>>> GetCustomersAsync(PagedListQueryBase query);
    }
}
