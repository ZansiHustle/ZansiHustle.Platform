using System;
using ZansiHustle.Shared.Enums.Wallets;

namespace ZansiHustle.Domain.Wallets
{
    /// <summary>
    /// An immutable wallet ledger entry — the source of truth for a customer's
    /// financial history. <see cref="BalanceAfter"/> snapshots the wallet balance
    /// right after this entry was applied. Idempotency for automatic credits is
    /// enforced on (<see cref="Type"/>, <see cref="ReferenceType"/>,
    /// <see cref="ReferenceId"/>) so a repeated trigger can't double-credit.
    /// </summary>
    public class WalletTransaction
    {
        public Guid Id { get; set; }

        public Guid WalletId { get; set; }
        public Guid UserId { get; set; }

        public WalletTransactionType Type { get; set; }
        public WalletTransactionDirection Direction { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";

        /// <summary>Wallet balance immediately after applying this entry.</summary>
        public decimal BalanceAfter { get; set; }

        /// <summary>What this entry references (e.g. "ServiceBooking", "Order").</summary>
        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }

        public string Description { get; set; } = string.Empty;

        public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Completed;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
