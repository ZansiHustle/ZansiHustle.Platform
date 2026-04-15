namespace ZansiHustle.Shared.Enums.Orders;

/// <summary>
/// Payment state of an order. v1 persists this field but payment capture is
/// not yet wired — all new orders default to <see cref="Pending"/>.
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}
