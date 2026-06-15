using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.CustomerFinance.Dtos
{
    /// <summary>
    /// Customer-facing spending overview. Money the customer has spent on
    /// ZansiHustle, with refunds shown separately. Avoids double-counting a
    /// wallet+external split: the ORDER total is the unit of spend, with
    /// <see cref="WalletSpent"/> / <see cref="ExternalSpent"/> as sub-components.
    /// Period spends are GROSS (charges in the window); refunds are reported
    /// separately so the customer can read net = AllTime − Refunded.
    /// </summary>
    public sealed class FinanceSummaryDto
    {
        public decimal WalletBalance { get; set; }

        /// <summary>Net all-time = gross all-time spend − refunds received.</summary>
        public decimal TotalSpent { get; set; }

        public decimal SpentThisWeek { get; set; }
        public decimal SpentThisMonth { get; set; }
        /// <summary>Gross all-time spend (before refunds).</summary>
        public decimal SpentAllTime { get; set; }

        public decimal TotalRefunded { get; set; }

        /// <summary>Wallet portion of gross spend (all time).</summary>
        public decimal WalletSpent { get; set; }
        /// <summary>External-gateway portion of gross spend (all time).</summary>
        public decimal ExternalSpent { get; set; }

        public decimal ProductSpend { get; set; }
        public decimal ServiceSpend { get; set; }

        public int TransactionCount { get; set; }
        public string Currency { get; set; } = "ZAR";
    }

    /// <summary>One combined finance transaction (order payment, refund, wallet move).</summary>
    public sealed class FinanceTransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }

        /// <summary>ProductOrderPayment | ServiceBookingPayment | Refund | PaymentReversal | Withdrawal | WalletCredit | WalletDebit.</summary>
        public string Type { get; set; } = string.Empty;
        /// <summary>Debit | Credit.</summary>
        public string Direction { get; set; } = string.Empty;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Reference { get; set; }
        public string Status { get; set; } = string.Empty;

        public Guid? RelatedOrderId { get; set; }
        public Guid? RelatedBookingId { get; set; }
        public Guid? RelatedWalletTransactionId { get; set; }

        /// <summary>Wallet | Ozow | WalletPlusOzow | Unknown.</summary>
        public string PaymentMethod { get; set; } = "Unknown";

        public bool IsRefund { get; set; }
        public bool IsPending { get; set; }
    }

    /// <summary>A page of combined finance transactions.</summary>
    public sealed class FinanceTransactionsPageDto
    {
        public List<FinanceTransactionDto> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public bool HasMore { get; set; }
        public string Currency { get; set; } = "ZAR";
    }
}
