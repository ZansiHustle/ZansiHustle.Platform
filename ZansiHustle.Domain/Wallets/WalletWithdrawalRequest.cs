using System;
using ZansiHustle.Shared.Enums.Wallets;

namespace ZansiHustle.Domain.Wallets
{
    /// <summary>
    /// A customer-initiated manual withdrawal of wallet funds to a bank account.
    /// V1 is request-only: on request the amount is HELD (a WithdrawalRequested
    /// debit reduces the wallet's AvailableBalance) so the same money can't be
    /// withdrawn twice; an admin later marks it Paid, or Rejected/Cancelled (which
    /// credits the held amount back). No automated payout.
    ///
    /// Security: we NEVER persist the full bank account number — only the last 4
    /// digits, for the customer to recognise the account. Nothing here is logged.
    /// </summary>
    public class WalletWithdrawalRequest
    {
        public Guid Id { get; set; }

        public Guid WalletId { get; set; }
        public Guid UserId { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";

        public string BankName { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        /// <summary>Last 4 digits only — the full number is never stored.</summary>
        public string AccountNumberLast4 { get; set; } = string.Empty;
        public string? BranchCode { get; set; }
        public WithdrawalAccountType AccountType { get; set; }

        public WithdrawalRequestStatus Status { get; set; } = WithdrawalRequestStatus.Pending;

        /// <summary>Optional customer note attached to the request.</summary>
        public string? Note { get; set; }
        /// <summary>Optional admin note set when processing/rejecting.</summary>
        public string? AdminNote { get; set; }

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAtUtc { get; set; }
        public Guid? ProcessedByUserId { get; set; }
    }
}
