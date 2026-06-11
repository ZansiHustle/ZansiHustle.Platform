using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Wallets.Dtos
{
    /// <summary>Wallet balance for the current user.</summary>
    public class WalletDto
    {
        public decimal AvailableBalance { get; set; }
        public string Currency { get; set; } = "ZAR";
    }

    /// <summary>A single ledger entry for the client.</summary>
    public class WalletTransactionDto
    {
        public Guid Id { get; set; }
        /// <summary>Enum name (BookingRejectedCredit / RefundCredit / …).</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>"Credit" or "Debit".</summary>
        public string Direction { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
        /// <summary>"Completed" / "Pending" / …</summary>
        public string Status { get; set; } = string.Empty;
        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Wallet balance + recent transactions (one round-trip for the screen).</summary>
    public class WalletSummaryDto
    {
        public WalletDto Wallet { get; set; } = new();
        public List<WalletTransactionDto> Transactions { get; set; } = new();
    }
}
