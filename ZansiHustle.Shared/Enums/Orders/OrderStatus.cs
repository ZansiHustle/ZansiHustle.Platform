namespace ZansiHustle.Shared.Enums.Orders;

/// <summary>
/// Lifecycle state of a marketplace order.
/// </summary>
public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5,

    /// <summary>
    /// Product order: payment has succeeded but the SELLER has not yet accepted
    /// (confirmed stock/availability). The customer's money is secured (Paid),
    /// but this is NOT "Confirmed" — dispatch must not start, and the order sits
    /// in the customer's "Pending" group / the seller's "New" requests until the
    /// seller accepts (→ Confirmed) or rejects (→ Cancelled + wallet refund).
    /// </summary>
    AwaitingSellerAcceptance = 6
}
