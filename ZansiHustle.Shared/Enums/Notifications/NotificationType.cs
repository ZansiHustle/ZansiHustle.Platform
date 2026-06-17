namespace ZansiHustle.Shared.Enums.Notifications;

/// <summary>
/// Kind of in-app notification. Drives the icon/copy on the client and, for
/// actionable types, the deep-link target encoded in the notification's
/// <c>DataJson</c> payload (targetType + ids). Integer-backed so adding a new
/// type never reshuffles existing rows.
/// </summary>
public enum NotificationType
{
    /// <summary>A buyer paid for a service booking — sent to the seller.</summary>
    SellerBookingRequested = 1,

    /// <summary>Seller accepted the booking — sent to the buyer.</summary>
    BookingAccepted = 2,

    /// <summary>Booking moved to in-progress — sent to the other party.</summary>
    BookingInProgress = 3,

    /// <summary>Booking completed — sent to the other party.</summary>
    BookingCompleted = 4,

    /// <summary>Booking rejected by the provider — sent to the buyer.</summary>
    BookingRejected = 5,

    /// <summary>A payment succeeded — sent to the buyer.</summary>
    PaymentSucceeded = 6,

    /// <summary>A payment failed — sent to the buyer.</summary>
    PaymentFailed = 7,

    /// <summary>Seller application/status changed — sent to the applicant.</summary>
    SellerStatusChanged = 8,

    /// <summary>New chat message — sent to the recipient (future).</summary>
    NewChatMessage = 9,

    /// <summary>Customer cancelled a booking before acceptance — sent to the provider.</summary>
    BookingCancelledByCustomer = 10,

    // ── Product order acceptance lifecycle ──────────────────────────────────
    /// <summary>A buyer paid for a PRODUCT order — sent to the seller (new request).</summary>
    SellerOrderRequested = 11,

    /// <summary>Payment secured, awaiting seller confirmation — sent to the buyer.</summary>
    OrderAwaitingSellerAcceptance = 12,

    /// <summary>Seller accepted the product order — sent to the buyer.</summary>
    OrderAcceptedBySeller = 13,

    /// <summary>Seller rejected the product order (refund issued) — sent to the buyer.</summary>
    OrderRejectedBySeller = 14,

    /// <summary>Courier/dispatch shipment reached a milestone (collected, in transit,
    /// out for delivery, delivered, needs-attention) — sent to the buyer and/or seller.</summary>
    ShipmentStatusChanged = 15,

    /// <summary>Anything without a specific actionable target.</summary>
    Generic = 100
}
