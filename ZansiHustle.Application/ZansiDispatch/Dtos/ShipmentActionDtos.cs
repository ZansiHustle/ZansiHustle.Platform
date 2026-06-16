using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch.Dtos
{
    /// <summary>One row in the shipment "Activity / Actions" timeline.</summary>
    public sealed class ShipmentActionDto
    {
        public Guid Id { get; set; }
        public Guid ShipmentId { get; set; }
        public Guid OrderId { get; set; }
        public Guid? ActorUserId { get; set; }
        public ZansiDispatchActor Actor { get; set; }
        public ZansiDispatchActionType ActionType { get; set; }
        public ZansiDispatchShipmentStatus? OldShipmentStatus { get; set; }
        public ZansiDispatchShipmentStatus? NewShipmentStatus { get; set; }
        public string? ProviderStatusBefore { get; set; }
        public string? ProviderStatusAfter { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>What the status-based cancellation decision did.</summary>
    public enum ZansiDispatchCancellationOutcome
    {
        /// <summary>Internal-only shipment cancelled (never provider-booked). Refund safe.</summary>
        CancelledInternal = 1,
        /// <summary>Provider cancellation succeeded. Refund safe.</summary>
        CancelledWithProvider = 2,
        /// <summary>Provider cancel failed → shipment NeedsAttention; do not refund yet.</summary>
        NeedsAttention = 3,
        /// <summary>Already collected/in transit → seller can't self-cancel; ops escalation.</summary>
        BlockedAlreadyCollected = 4,
    }

    /// <summary>Result of a status-based shipment cancellation.</summary>
    public sealed class DispatchCancellationResultDto
    {
        public ZansiDispatchCancellationOutcome Outcome { get; set; }
        /// <summary>True only when it's safe for the order layer to refund the buyer.</summary>
        public bool CanRefund { get; set; }
        /// <summary>Customer-safe message (no raw provider errors).</summary>
        public string Message { get; set; } = string.Empty;
        public ZansiDispatchShipmentStatus? ShipmentStatus { get; set; }
    }

    /// <summary>Seller/admin pickup-reschedule request body.</summary>
    public sealed class ReschedulePickupRequestDto
    {
        public DateTime NewPickupDateUtc { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>Customer delivery-date-change request body.</summary>
    public sealed class RequestDeliveryChangeRequestDto
    {
        public DateTime NewDeliveryDateUtc { get; set; }
        public string? Reason { get; set; }
    }
}
