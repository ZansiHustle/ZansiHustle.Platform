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
        Task<Result<OrderDto>> AcceptAsync(Guid userId, Guid orderId, AcceptOrderRequestDto? request = null);

        /// <summary>
        /// Seller rejects a paid PRODUCT order awaiting acceptance
        /// (AwaitingSellerAcceptance → Cancelled). Requires a reason, refunds the
        /// customer to their wallet, and notifies them. Seller/merchant owner only.
        /// </summary>
        Task<Result<OrderDto>> RejectAsync(Guid userId, Guid orderId, RejectOrderRequestDto request);

        /// <summary>
        /// Seller cancels fulfilment AFTER acceptance (Confirmed/InProgress). The
        /// decision is status-based via ZansiDispatch: internal/uncollected →
        /// cancel + refund; already collected/in transit → blocked + ops escalation;
        /// provider cancel failed → shipment NeedsAttention (order unchanged).
        /// </summary>
        Task<Result<OrderDto>> CancelFulfilmentAsync(Guid userId, Guid orderId, RejectOrderRequestDto? request);

        /// <summary>Seller requests a pickup reschedule for an accepted order's shipment.</summary>
        Task<Result<ZansiHustle.Application.ZansiDispatch.Dtos.ShipmentDto>> ReschedulePickupAsync(Guid userId, Guid orderId, ZansiHustle.Application.ZansiDispatch.Dtos.ReschedulePickupRequestDto request);

        /// <summary>Customer requests a delivery-date change for their order's shipment.</summary>
        Task<Result<ZansiHustle.Application.ZansiDispatch.Dtos.ShipmentDto>> RequestDeliveryChangeAsync(Guid userId, Guid orderId, ZansiHustle.Application.ZansiDispatch.Dtos.RequestDeliveryChangeRequestDto request);

        Task<Result<OrderDto>> UpdateStatusAsync(Guid userId, Guid orderId, UpdateOrderStatusRequestDto request);
    }
}
