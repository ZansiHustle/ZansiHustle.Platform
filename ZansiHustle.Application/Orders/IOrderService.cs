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

        /// <summary>
        /// Customer-facing order tracking (product orders). Caller must be the
        /// buyer or merchant owner. Returns a privacy-safe DTO (shop name +
        /// buyer destination only, no seller contact/address) with the dispatch
        /// status and a checkpoint timeline.
        /// </summary>
        Task<Result<OrderTrackingDto>> GetTrackingAsync(Guid userId, Guid orderId);

        /// <summary>
        /// Seller accepts a paid PRODUCT order awaiting acceptance
        /// (AwaitingSellerAcceptance → Confirmed). Creates the ZansiDispatch
        /// shipment NOW (not at payment) and notifies the customer. Seller/merchant
        /// owner only; idempotent-guarded against double accept.
        /// </summary>
        Task<Result<OrderDto>> AcceptAsync(Guid userId, Guid orderId);

        /// <summary>
        /// Seller rejects a paid PRODUCT order awaiting acceptance
        /// (AwaitingSellerAcceptance → Cancelled). Requires a reason, refunds the
        /// customer to their wallet, and notifies them. Seller/merchant owner only.
        /// </summary>
        Task<Result<OrderDto>> RejectAsync(Guid userId, Guid orderId, RejectOrderRequestDto request);

        Task<Result<OrderDto>> UpdateStatusAsync(Guid userId, Guid orderId, UpdateOrderStatusRequestDto request);
    }
}
