using System;
using System.Threading.Tasks;
using ZansiHustle.Application.CustomerFinance.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.CustomerFinance
{
    /// <summary>
    /// Customer financial overview — combines a buyer's orders (product +
    /// service spend), the wallet ledger (refunds, withdrawals, adjustments) and
    /// the wallet balance into one customer-safe view. Never exposes seller
    /// earnings/payout, provider secrets, or other users' data.
    /// </summary>
    public interface ICustomerFinanceService
    {
        Task<Result<FinanceSummaryDto>> GetSummaryAsync(Guid userId);

        Task<Result<FinanceTransactionsPageDto>> GetTransactionsAsync(
            Guid userId, string? type, string? range, int page, int pageSize);
    }
}
