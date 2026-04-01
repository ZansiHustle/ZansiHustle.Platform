using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.BudgetTransactions.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.BudgetTransactions
{
    /// <summary>
    /// Service contract for budget transaction operations.
    /// </summary>
    public interface IBudgetTransactionService
    {
        Task<Result<List<BudgetTransactionListItemDto>>> GetAllAsync();
        Task<Result<BudgetTransactionDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<BudgetTransactionDetailsDto>> CreateAsync(CreateBudgetTransactionRequestDto request);
        Task<Result<BudgetTransactionDetailsDto>> UpdateAsync(Guid id, UpdateBudgetTransactionRequestDto request);
        Task<Result> DeleteAsync(Guid id);
    }
}
