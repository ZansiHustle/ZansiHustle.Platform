namespace ZansiHustle.Shared.Enums.Wallets;

/// <summary>
/// Reason a wallet ledger entry exists. Integer-backed so adding a kind never
/// reshuffles existing rows. WalletPaymentDebit is reserved for the future
/// "pay with wallet" feature and is NOT used yet.
/// </summary>
public enum WalletTransactionType
{
    /// <summary>Customer credited because a provider rejected a paid booking.</summary>
    BookingRejectedCredit = 1,

    /// <summary>Customer credited because an order was cancelled.</summary>
    OrderCancelledCredit = 2,

    /// <summary>Generic refund credit (admin/manual or other flows).</summary>
    RefundCredit = 3,

    /// <summary>Manual balance correction by an admin.</summary>
    AdminAdjustment = 4,

    /// <summary>Customer requested a withdrawal (debit, pending payout).</summary>
    WithdrawalRequested = 5,

    /// <summary>A withdrawal was paid out.</summary>
    WithdrawalPaid = 6,

    /// <summary>Customer credited because they cancelled a paid booking
    /// (V1: only allowed while still Requested / awaiting provider).</summary>
    BookingCancelledCredit = 7,

    /// <summary>Wallet balance applied toward an order/booking at checkout (debit).</summary>
    WalletPaymentDebit = 100,

    /// <summary>Wallet payment reversed (credit) when the external payment for a
    /// split/partial order failed or was cancelled — restores the held balance.</summary>
    WalletPaymentReversal = 101
}
