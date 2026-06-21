namespace ZansiHustle.Shared.Enums.Finance;

/// <summary>
/// Settlement state of a seller ledger entry.
/// </summary>
public enum SellerLedgerStatus
{
    /// <summary>Paid order/booking but work not yet completed — not payable yet.</summary>
    Pending = 1,

    /// <summary>Work completed — eligible for payout.</summary>
    Available = 2,

    /// <summary>Entry reversed (refund/cancellation).</summary>
    Reversed = 3,

    /// <summary>Already paid out to the seller.</summary>
    PaidOut = 4
}
