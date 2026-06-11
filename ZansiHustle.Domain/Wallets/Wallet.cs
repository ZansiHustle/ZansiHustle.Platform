using System;

namespace ZansiHustle.Domain.Wallets
{
    /// <summary>
    /// A customer's wallet. <see cref="AvailableBalance"/> is a denormalised
    /// running total kept in lockstep with the <c>WalletTransaction</c> ledger
    /// (the ledger is the source of truth). One wallet per user; created lazily
    /// on the first credit or the first fetch. ZAR only for now.
    /// </summary>
    public class Wallet
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string Currency { get; set; } = "ZAR";

        /// <summary>Spendable balance. Updated atomically with each ledger entry.</summary>
        public decimal AvailableBalance { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
