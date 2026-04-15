using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Orders.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Orders
{
    /// <summary>
    /// Service contract for order (buyer purchase / seller fulfilment) operations.
    /// All write paths enforce role-based authorisation via the caller's user id.
    /// </summary>
    public interface IOrderService
    {
        Task<Result<OrderDto>> CreateAsync(Guid buyerUserId, CreateOrderRequestDto request);

        Task<Result<List<OrderListItemDto>>> GetMineAsync(Guid buyerUserId);

        Task<Result<List<OrderListItemDto>>> GetForSellerAsync(Guid sellerUserId);

        Task<Result<OrderDto>> GetByIdAsync(Guid userId, Guid orderId);

        Task<Result<OrderDto>> UpdateStatusAsync(Guid userId, Guid orderId, UpdateOrderStatusRequestDto request);
    }
}
