namespace ZansiHustle.Shared.Enums.Wallets;

/// <summary>Lifecycle of a wallet ledger entry. Credits applied immediately are
/// <see cref="Completed"/>; withdrawals start <see cref="Pending"/>.</summary>
public enum WalletTransactionStatus
{
    Completed = 1,
    Pending = 2,
    Failed = 3,
    Reversed = 4
}
