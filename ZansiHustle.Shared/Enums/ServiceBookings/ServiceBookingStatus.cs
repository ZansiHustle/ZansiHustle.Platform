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

    /// <summary>Payment succeeded and the booking is now awaiting provider
    /// acceptance. This is the post-payment state in the pay-first flow
    /// (PendingPayment → Requested on paid). Firmly blocks the slot.</summary>
    Requested = 7,

    /// <summary>Both parties (provider AND customer) have marked the booking as
    /// started on/after the scheduled day. Blocks the slot.</summary>
    InProgress = 8
}
