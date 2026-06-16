using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Domain.ZansiDispatch
{
    /// <summary>
    /// One audited shipment lifecycle action — who did what, the status
    /// transition it caused, and (for provider calls) a redacted provider
    /// response. Drives the "Activity / Actions" timeline in the ZansiDispatch
    /// detail drawer. Append-only; never updated. All timestamps UTC.
    /// </summary>
    public class ZansiDispatchShipmentAction
    {
        public Guid Id { get; set; }

        public Guid ShipmentId { get; set; }
        public Guid OrderId { get; set; }

        /// <summary>Null for System/Webhook actions; otherwise the acting user.</summary>
        public Guid? ActorUserId { get; set; }
        public ZansiDispatchActor Actor { get; set; }

        public ZansiDispatchActionType ActionType { get; set; }

        public ZansiDispatchShipmentStatus? OldShipmentStatus { get; set; }
        public ZansiDispatchShipmentStatus? NewShipmentStatus { get; set; }

        public string? ProviderStatusBefore { get; set; }
        public string? ProviderStatusAfter { get; set; }

        public string? Reason { get; set; }
        public string? Notes { get; set; }

        /// <summary>Redacted provider response (no secrets/PII). Never shown to customers.</summary>
        public string? SafeProviderResponseJson { get; set; }

        /// <summary>Client/request correlation id when available.</summary>
        public string? CorrelationId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
