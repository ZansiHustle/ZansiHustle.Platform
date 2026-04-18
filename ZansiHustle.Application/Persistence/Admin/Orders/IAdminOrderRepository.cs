using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Orders.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Persistence.Admin.Orders
{
    /// <summary>
    /// Aggregation + listing queries for the admin Orders page.
    /// </summary>
    public interface IAdminOrderRepository
    {
        Task<OrdersKpisDto> GetKpisAsync();
        Task<PagedResult<AdminOrderListItemDto>> GetPagedAsync(PagedListQueryBase query);
    }
}
