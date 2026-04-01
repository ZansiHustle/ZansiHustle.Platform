using System;
using ZansiHustle.Shared.Enums.Budget;

namespace ZansiHustle.Application.BudgetTransactions.Dtos
{
    /// <summary>
    /// Lightweight budget transaction DTO for list screens.
    /// </summary>
    public class BudgetTransactionListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateTime TransactionDateUtc { get; set; }
        public BudgetTransactionType TransactionType { get; set; }
        public BudgetCategory Category { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
