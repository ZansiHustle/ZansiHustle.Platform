namespace ZansiHustle.Shared.Enums.Finance;

/// <summary>
/// Origin of a finance ledger entry (seller or platform book).
/// </summary>
public enum LedgerSourceType
{
    ProductOrder = 1,
    ServiceBooking = 2,
    Adjustment = 3,
    Refund = 4,
    Payout = 5
}
