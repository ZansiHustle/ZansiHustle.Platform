using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Admin.Orders.Dtos;
using ZansiHustle.Application.Persistence.Admin.Orders;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Admin.Orders
{
    /// <summary>
    /// Thin pass-through service. Error handling is centralized here so the
    /// controller never leaks EF exceptions.
    /// </summary>
    public class AdminOrderService : IAdminOrderService
    {
        private readonly IAdminOrderRepository _repository;

        public AdminOrderService(IAdminOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<OrdersKpisDto>> GetKpisAsync()
        {
            try
            {
                var data = await _repository.GetKpisAsync();
                return Result<OrdersKpisDto>.Success(data, "Orders KPIs retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<OrdersKpisDto>.Failure($"Failed to retrieve orders KPIs. {ex.Message}");
            }
        }

        public async Task<Result<PagedResult<AdminOrderListItemDto>>> GetOrdersAsync(PagedListQueryBase query)
        {
            var safe = PagedQueryGuard.Clamp(query);

            try
            {
                var data = await _repository.GetPagedAsync(safe);
                return Result<PagedResult<AdminOrderListItemDto>>.Success(data, "Orders retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<PagedResult<AdminOrderListItemDto>>.Failure($"Failed to retrieve orders. {ex.Message}");
            }
        }
    }
}
