using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Orders.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Orders
{
    /// <summary>
    /// Service contract for the cross-merchant admin Orders views.
    /// </summary>
    public interface IAdminOrderService
    {
        Task<Result<OrdersKpisDto>> GetKpisAsync();
        Task<Result<PagedResult<AdminOrderListItemDto>>> GetOrdersAsync(PagedListQueryBase query);
    }
}
