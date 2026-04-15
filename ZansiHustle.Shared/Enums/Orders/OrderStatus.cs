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
    Cancelled = 5
}
