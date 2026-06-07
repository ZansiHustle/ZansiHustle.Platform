using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Infrastructure.ZansiDispatch.Providers.CourierGuy
{
    /// <summary>
    /// Maps Courier Guy / Shiplogic tracking status strings to ZansiDispatch's
    /// internal <see cref="ZansiDispatchShipmentStatus"/>. Applies the documented
    /// "visual" collapses (e.g. collection-unassigned → collection-assigned)
    /// before mapping, so internal/buyer-facing statuses stay clean. Unknown
    /// statuses fall back to <see cref="ZansiDispatchShipmentStatus.InTransit"/>
    /// (the safe "in progress" bucket) rather than throwing.
    /// </summary>
    public static class CourierGuyStatusMap
    {
        /// <summary>Normalise + map a raw provider status to an internal status.</summary>
        public static ZansiDispatchShipmentStatus Map(string? providerStatus)
        {
            var s = (providerStatus ?? string.Empty).Trim().ToLowerInvariant();

            // Visual collapses documented by Courier Guy.
            s = s switch
            {
                "collection-unassigned" => "collection-assigned",
                "collection-rejected" => "collection-assigned",
                "delivery-unassigned" => "delivery-assigned",
                "delivery-rejected" => "delivery-assigned",
                "on-hold-internal" => "at-hub",
                _ => s,
            };

            return s switch
            {
                "submitted" => ZansiDispatchShipmentStatus.BookedWithCourier,
                "manifested" => ZansiDispatchShipmentStatus.BookedWithCourier,
                "ready-for-dispatch" => ZansiDispatchShipmentStatus.PreparingPickup,

                "collection-assigned" => ZansiDispatchShipmentStatus.PreparingPickup,
                "collection-exception" => ZansiDispatchShipmentStatus.Exception,
                "collection-failed-attempt" => ZansiDispatchShipmentStatus.Exception,
                "collected" => ZansiDispatchShipmentStatus.PickedUp,
                "awaiting-dropoff" => ZansiDispatchShipmentStatus.PickedUp,

                "at-hub" => ZansiDispatchShipmentStatus.InTransit,
                "returned-to-hub" => ZansiDispatchShipmentStatus.InTransit,
                "in-transit" => ZansiDispatchShipmentStatus.InTransit,
                "at-destination-hub" => ZansiDispatchShipmentStatus.InTransit,
                "floor-check" => ZansiDispatchShipmentStatus.InTransit,

                "on-hold" => ZansiDispatchShipmentStatus.OnHold,

                "delivery-assigned" => ZansiDispatchShipmentStatus.OutForDelivery,
                "out-for-delivery" => ZansiDispatchShipmentStatus.OutForDelivery,
                "ready-for-pickup" => ZansiDispatchShipmentStatus.OutForDelivery,
                "delivery-exception" => ZansiDispatchShipmentStatus.Exception,
                "delivery-failed-attempt" => ZansiDispatchShipmentStatus.Exception,

                "delivered" => ZansiDispatchShipmentStatus.Delivered,

                "returned-to-sender" => ZansiDispatchShipmentStatus.Returned,
                "undeliverable" => ZansiDispatchShipmentStatus.Failed,
                "cancelled" => ZansiDispatchShipmentStatus.Cancelled,

                _ => ZansiDispatchShipmentStatus.InTransit,
            };
        }
    }
}
