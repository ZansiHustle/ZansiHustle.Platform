namespace ZansiHustle.Shared.Enums.ServiceBookings;

/// <summary>
/// Lifecycle state of a single service booking (one scheduled slot tied to an
/// <c>Order</c>). Drives availability blocking: only the "active hold" states
/// remove a slot from the buyer calendar.
/// </summary>
public enum ServiceBookingStatus
{
    /// <summary>Created with the order, payment not yet confirmed. Blocks the
    /// slot ONLY for a short hold window (see BookingAvailabilityDefaults) so a
    /// stale/abandoned checkout doesn't pin a slot forever.</summary>
    PendingPayment = 1,

    /// <summary>Payment confirmed. Firmly blocks the slot.</summary>
    Confirmed = 2,

    /// <summary>Seller explicitly accepted the booking (future seller workflow).
    /// Blocks the slot.</summary>
    Accepted = 3,

    /// <summary>Service delivered. Blocks the (historical) slot.</summary>
    Completed = 4,

    /// <summary>Buyer/seller cancelled. Does NOT block — slot is released.</summary>
    Cancelled = 5,

    /// <summary>Seller rejected the booking. Does NOT block — slot is released.</summary>
    Rejected = 6,

    /// <summary>Buyer requested but no payment yet (reserved for a future
    /// request-first workflow). Not used by the current pay-first flow.</summary>
    Requested = 7
}
