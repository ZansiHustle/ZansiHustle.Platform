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

    /// <summary>Customer's withdrawal-request body. The full account number is used
    /// ONLY to derive the last 4 digits server-side and is never persisted/logged.</summary>
    public class CreateWithdrawalRequestDto
    {
        public string BankName { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string? BranchCode { get; set; }
        /// <summary>"Cheque" | "Savings" | "Business".</summary>
        public string AccountType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>A withdrawal request as returned to the client. The account number
    /// is masked (****1234) — the full value is never exposed.</summary>
    public class WithdrawalRequestDto
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string BankName { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        /// <summary>Masked, e.g. "****1234".</summary>
        public string AccountNumberMasked { get; set; } = string.Empty;
        public string? BranchCode { get; set; }
        public string AccountType { get; set; } = string.Empty;
        /// <summary>"Pending" / "Approved" / "Paid" / "Rejected" / "Cancelled".</summary>
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? AdminNote { get; set; }
        public DateTime RequestedAtUtc { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
    }
}
