namespace ZansiHustle.Shared.Enums.Payments;

/// <summary>
/// Lifecycle state of a single payment attempt (one row per attempt against an order).
/// Deliberately finer-grained than <see cref="Orders.PaymentStatus"/> so the two
/// can evolve independently — order-level payment status is an aggregate signal
/// for buyers/sellers, transaction-level status is for accounting + support.
/// </summary>
public enum PaymentTransactionStatus
{
    /// <summary>Payment row created locally, not yet sent to provider.</summary>
    Initialized = 1,

    /// <summary>Authorization URL issued; user redirected to the provider checkout.</summary>
    Pending = 2,

    /// <summary>Provider confirmed successful charge.</summary>
    Succeeded = 3,

    /// <summary>Provider reported a failed attempt (insufficient funds, rejected card, etc.).</summary>
    Failed = 4,

    /// <summary>User abandoned checkout or we cancelled locally before completion.</summary>
    Cancelled = 5,

    /// <summary>Provider-confirmed refund applied.</summary>
    Refunded = 6
}
