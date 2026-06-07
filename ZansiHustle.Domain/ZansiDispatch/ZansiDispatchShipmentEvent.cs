using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Domain.ZansiDispatch
{
    /// <summary>
    /// A single tracking event for a shipment — from a provider tracking poll or
    /// an inbound webhook. Stores the raw provider status string alongside the
    /// mapped internal status so the timeline is faithful even for statuses we
    /// don't surface to buyers. Append-only. All timestamps are UTC.
    /// </summary>
    public class ZansiDispatchShipmentEvent
    {
        public Guid Id { get; set; }

        public Guid ShipmentId { get; set; }

        public ZansiDispatchProviderType ProviderType { get; set; }
        /// <summary>Provider-side event id, when supplied — used to de-dupe.</summary>
        public string? ProviderEventId { get; set; }

        /// <summary>Raw provider status string (e.g. "out-for-delivery").</summary>
        public string ProviderStatus { get; set; } = string.Empty;
        /// <summary>Mapped internal status at the time of this event.</summary>
        public ZansiDispatchShipmentStatus InternalStatus { get; set; }

        public string? Message { get; set; }
        public string? Location { get; set; }

        public DateTime EventTime { get; set; }

        public string? RawEventJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
