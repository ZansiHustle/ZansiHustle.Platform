namespace ZansiHustle.Shared.Enums.Wallets;

/// <summary>
/// Lifecycle of a manual wallet withdrawal request. V1 is request-only — the
/// money is held (debited) on request; an admin later moves it Approved → Paid
/// or Rejected (reversal credit). Integer-backed so adding a state never
/// reshuffles existing rows.
/// </summary>
public enum WithdrawalRequestStatus
{
    /// <summary>Submitted by the customer, awaiting manual review.</summary>
    Pending = 1,

    /// <summary>Reviewed and approved; payout not yet sent.</summary>
    Approved = 2,

    /// <summary>Paid out to the customer's bank account.</summary>
    Paid = 3,

    /// <summary>Rejected by an admin — the held amount is credited back.</summary>
    Rejected = 4,

    /// <summary>Cancelled by the customer before processing — held amount returned.</summary>
    Cancelled = 5
}
