using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.ZansiDispatch.Dtos;
using ZansiHustle.Shared.Enums.ZansiDispatch;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ZansiDispatch
{
    /// <summary>
    /// ZansiDispatch — the ZansiHustle logistics control layer. Phase 1:
    /// checkout delivery quotes, quote selection, the order→shipment bridge,
    /// and the admin command centre (shipments, actual-cost capture,
    /// reconciliation, status). Provider-agnostic: quoting goes through the
    /// provider abstraction so CourierGuy/Shiplogic can drop in later.
    ///
    /// All methods return the standard <see cref="Result"/> envelope and never
    /// throw to the caller.
    /// </summary>
    public interface IZansiDispatchService
    {
        // ── Checkout quoting ────────────────────────────────────────────────
        /// <param name="correlationId">Optional client-supplied id (X-Correlation-Id)
        /// echoed into the UAT quote debug logs so a request can be matched across
        /// app → API → provider.</param>
        Task<Result<QuoteDto>> CreateQuoteAsync(Guid userId, CreateQuoteRequestDto request, CancellationToken ct = default, string? correlationId = null);
        Task<Result<QuoteDto>> SelectOptionAsync(Guid userId, bool isAdmin, Guid quoteId, Guid quoteOptionId, CancellationToken ct = default);

        // ── Order-creation bridge ───────────────────────────────────────────
        /// <summary>Validates a quote option belongs to the user and is unexpired; returns the resolved fee/context for order creation.</summary>
        Task<Result<SelectableQuoteOptionDto>> GetSelectableOptionAsync(Guid userId, Guid quoteOptionId, CancellationToken ct = default);
        /// <summary>Creates the shipment + QuoteCharged ledger entry for a freshly-created order. Best-effort: idempotent, never throws.</summary>
        Task CreateShipmentForOrderAsync(CreateShipmentForOrderInput input, CancellationToken ct = default);

        /// <summary>
        /// Creates the PendingDispatch shipment + QuoteCharged ledger entry for an
        /// order that has just been PAID, resolving the provider/service/addresses
        /// from the order's stored delivery quote option. Idempotent (no-op if a
        /// shipment already exists for the order) and best-effort (never throws).
        /// Does NOT re-check quote expiry — payment can land after the quote's
        /// short TTL; the fee was already locked onto the order at creation.
        /// Called from the payment-success path, not order creation, so unpaid
        /// orders never create shipment/ledger rows.
        /// </summary>
        Task CreateShipmentForPaidOrderAsync(
            Guid orderId, Guid userId, Guid? merchantId, Guid quoteOptionId,
            decimal quotedDeliveryFee, string? dropoffAddressSummary, CancellationToken ct = default);

        // ── Shipment lifecycle (provider-backed) ────────────────────────────
        /// <summary>Books a real shipment for an order from its selected quote option (CourierGuy when applicable).</summary>
        Task<Result<ShipmentDto>> CreateShipmentFromQuoteAsync(Guid adminUserId, CreateShipmentFromQuoteRequestDto request, CancellationToken ct = default);
        /// <summary>
        /// Retries a failed / NeedsAttention courier booking using STORED
        /// quote/order/merchant/customer data (no manual payload). Admin-only,
        /// idempotent, obeys every guard + the kill switch.
        /// </summary>
        Task<Result<ShipmentDto>> RetryBookingAsync(Guid adminUserId, Guid shipmentId, CancellationToken ct = default);
        /// <summary>
        /// Automatically books the courier after a seller ACCEPTS a paid product
        /// order, IF <c>CourierGuy.AutoBookAfterSellerAcceptance</c> is enabled.
        /// Best-effort: a failure is recorded on the shipment (NeedsAttention) and
        /// never thrown — seller acceptance always succeeds. Never bypasses the
        /// kill switch or any booking guard, and never runs in the payment webhook.
        /// </summary>
        Task AutoBookForAcceptedOrderAsync(Guid orderId, CancellationToken ct = default);
        /// <summary>Polls the provider, records events, and returns the current tracking timeline.</summary>
        Task<Result<TrackingResultDto>> TrackShipmentAsync(Guid shipmentId, CancellationToken ct = default);
        /// <summary>
        /// CUSTOMER-SAFE read of an order's dispatch state from STORED data only
        /// (no live provider poll, no seller/cost/reconciliation fields). Returns
        /// a snapshot with <c>HasShipment=false</c> when dispatch hasn't started.
        /// Callers must enforce buyer ownership before calling this.
        /// </summary>
        Task<Result<OrderDispatchSnapshotDto>> GetOrderDispatchSnapshotAsync(Guid orderId, CancellationToken ct = default);
        /// <summary>Cancels a shipment with the provider (when booked) and internally.</summary>
        Task<Result<ShipmentDto>> CancelShipmentAsync(Guid adminUserId, Guid shipmentId, CancelShipmentRequestDto? request, CancellationToken ct = default);
        /// <summary>Fetches the signed label/waybill URL from the provider (admin/ops only).</summary>
        Task<Result<ShipmentLabelDto>> GetShipmentLabelAsync(Guid shipmentId, CancellationToken ct = default);

        // ── Post-acceptance lifecycle (status-based; audited) ───────────────
        /// <summary>Refresh live courier status from the provider + log the action (admin).</summary>
        Task<Result<ShipmentDto>> RefreshStatusAsync(Guid adminUserId, Guid shipmentId, CancellationToken ct = default);
        /// <summary>Admin status-based provider cancellation (refresh → cancel/block/needs-attention).</summary>
        Task<Result<ShipmentDto>> CancelProviderAsync(Guid adminUserId, Guid shipmentId, CancelShipmentRequestDto? request, CancellationToken ct = default);
        /// <summary>Cancel the shipment for an order (status-based). Tells the order layer whether a refund is safe.</summary>
        Task<Result<DispatchCancellationResultDto>> TryCancelForOrderAsync(Guid orderId, ZansiDispatchActor actor, Guid? actorUserId, string? reason, CancellationToken ct = default);
        /// <summary>Seller/admin pickup reschedule (provider call if supported, else ops task — never faked).</summary>
        Task<Result<ShipmentDto>> ReschedulePickupAsync(Guid actorUserId, ZansiDispatchActor actor, Guid shipmentId, ReschedulePickupRequestDto request, CancellationToken ct = default);
        /// <summary>Customer delivery-date-change REQUEST (provider call if supported, else ops task).</summary>
        Task<Result<ShipmentDto>> RequestDeliveryChangeAsync(Guid customerUserId, Guid shipmentId, RequestDeliveryChangeRequestDto request, CancellationToken ct = default);
        /// <summary>The shipment "Activity / Actions" audit timeline.</summary>
        Task<Result<List<ShipmentActionDto>>> GetShipmentActionsAsync(Guid shipmentId, CancellationToken ct = default);
        /// <summary>Order-scoped pickup reschedule (seller, resolves the shipment from the order).</summary>
        Task<Result<ShipmentDto>> ReschedulePickupForOrderAsync(Guid actorUserId, ZansiDispatchActor actor, Guid orderId, ReschedulePickupRequestDto request, CancellationToken ct = default);
        /// <summary>Order-scoped customer delivery-change request (resolves the shipment from the order).</summary>
        Task<Result<ShipmentDto>> RequestDeliveryChangeForOrderAsync(Guid customerUserId, Guid orderId, RequestDeliveryChangeRequestDto request, CancellationToken ct = default);
        /// <summary>Processes an inbound courier webhook payload — matches a shipment and records events.</summary>
        Task<Result<WebhookAckDto>> HandleCourierWebhookAsync(string rawBody, string? authHeader, CancellationToken ct = default);

        // ── Command centre (admin/ops) ──────────────────────────────────────
        Task<Result<CommandCentreOverviewDto>> GetOverviewAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
        Task<Result<List<ShipmentListItemDto>>> GetShipmentsAsync(ZansiDispatchShipmentStatus? status, int page, int pageSize, CancellationToken ct = default);
        Task<Result<ShipmentDto>> GetShipmentAsync(Guid shipmentId, CancellationToken ct = default);
        Task<Result<ShipmentDto>> CaptureActualCostAsync(Guid adminUserId, Guid shipmentId, CaptureActualCostRequestDto request, CancellationToken ct = default);
        Task<Result<ShipmentDto>> UpdateShipmentStatusAsync(Guid adminUserId, Guid shipmentId, UpdateShipmentStatusRequestDto request, CancellationToken ct = default);
    }
}
