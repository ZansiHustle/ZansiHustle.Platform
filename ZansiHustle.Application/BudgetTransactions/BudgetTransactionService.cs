using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.BudgetTransactions.Dtos;
using ZansiHustle.Application.Persistence.BudgetTransactions;
using ZansiHustle.Domain.BudgetTransactions;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.BudgetTransactions
{
    /// <summary>
    /// Provides business logic for budget transaction operations.
    /// </summary>
    public class BudgetTransactionService : IBudgetTransactionService
    {
        private readonly IBudgetTransactionRepository _budgetTransactionRepository;

        /// <summary>
        /// Creates a new instance of the <see cref="BudgetTransactionService"/> class.
        /// </summary>
        public BudgetTransactionService(IBudgetTransactionRepository budgetTransactionRepository)
        {
            _budgetTransactionRepository = budgetTransactionRepository;
        }

        /// <inheritdoc />
        public async Task<Result<List<BudgetTransactionListItemDto>>> GetAllAsync()
        {
            try
            {
                var transactions = await _budgetTransactionRepository.GetAllAsync();
                var data = transactions.Select(MapToListItemDto).ToList();

                return Result<List<BudgetTransactionListItemDto>>.Success(data, "Budget transactions retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<List<BudgetTransactionListItemDto>>.Failure($"An error occurred while retrieving budget transactions. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BudgetTransactionDetailsDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var transaction = await _budgetTransactionRepository.GetByIdAsync(id);

                if (transaction is null)
                {
                    return Result<BudgetTransactionDetailsDto>.Failure("Budget transaction not found.");
                }

                return Result<BudgetTransactionDetailsDto>.Success(MapToDetailsDto(transaction), "Budget transaction retrieved successfully.");
            }
            catch (Exception ex)
            {
                return Result<BudgetTransactionDetailsDto>.Failure($"An error occurred while retrieving the budget transaction. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BudgetTransactionDetailsDto>> CreateAsync(CreateBudgetTransactionRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<BudgetTransactionDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return Result<BudgetTransactionDetailsDto>.Failure("Description is required.");
                }

                var entity = new BudgetTransaction
                {
                    Id = Guid.NewGuid(),
                    Code = $"BGT-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    TransactionDateUtc = request.TransactionDateUtc,
                    TransactionType = request.TransactionType,
                    Category = request.Category,
                    Description = request.Description.Trim(),
                    Amount = request.Amount,
                    Reference = request.Reference?.Trim(),
                    RecordedByUserId = request.RecordedByUserId,
                    RelatedEntityType = request.RelatedEntityType?.Trim(),
                    RelatedEntityId = request.RelatedEntityId,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _budgetTransactionRepository.AddAsync(entity);

                var saved = await _budgetTransactionRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<BudgetTransactionDetailsDto>.Failure("Failed to create budget transaction.");
                }

                return Result<BudgetTransactionDetailsDto>.Success(MapToDetailsDto(entity), "Budget transaction created successfully.");
            }
            catch (Exception ex)
            {
                return Result<BudgetTransactionDetailsDto>.Failure($"An error occurred while creating the budget transaction. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<BudgetTransactionDetailsDto>> UpdateAsync(Guid id, UpdateBudgetTransactionRequestDto request)
        {
            try
            {
                if (request is null)
                {
                    return Result<BudgetTransactionDetailsDto>.Failure("Request is required.");
                }

                if (string.IsNullOrWhiteSpace(request.Description))
                {
                    return Result<BudgetTransactionDetailsDto>.Failure("Description is required.");
                }

                var transaction = await _budgetTransactionRepository.GetByIdAsync(id);

                if (transaction is null)
                {
                    return Result<BudgetTransactionDetailsDto>.Failure("Budget transaction not found.");
                }

                transaction.TransactionDateUtc = request.TransactionDateUtc;
                transaction.TransactionType = request.TransactionType;
                transaction.Category = request.Category;
                transaction.Description = request.Description.Trim();
                transaction.Amount = request.Amount;
                transaction.Reference = request.Reference?.Trim();
                transaction.RecordedByUserId = request.RecordedByUserId;
                transaction.RelatedEntityType = request.RelatedEntityType?.Trim();
                transaction.RelatedEntityId = request.RelatedEntityId;

                _budgetTransactionRepository.Update(transaction);

                var saved = await _budgetTransactionRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result<BudgetTransactionDetailsDto>.Failure("Failed to update budget transaction.");
                }

                return Result<BudgetTransactionDetailsDto>.Success(MapToDetailsDto(transaction), "Budget transaction updated successfully.");
            }
            catch (Exception ex)
            {
                return Result<BudgetTransactionDetailsDto>.Failure($"An error occurred while updating the budget transaction. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result> DeleteAsync(Guid id)
        {
            try
            {
                var transaction = await _budgetTransactionRepository.GetByIdAsync(id);

                if (transaction is null)
                {
                    return Result.Failure("Budget transaction not found.");
                }

                _budgetTransactionRepository.Delete(transaction);

                var saved = await _budgetTransactionRepository.SaveChangesAsync();

                if (!saved)
                {
                    return Result.Failure("Failed to delete budget transaction.");
                }

                return Result.Success("Budget transaction deleted successfully.");
            }
            catch (Exception ex)
            {
                return Result.Failure($"An error occurred while deleting the budget transaction. {ex.Message}");
            }
        }

        private static BudgetTransactionListItemDto MapToListItemDto(BudgetTransaction transaction)
        {
            return new BudgetTransactionListItemDto
            {
                Id = transaction.Id,
                Code = transaction.Code,
                TransactionDateUtc = transaction.TransactionDateUtc,
                TransactionType = transaction.TransactionType,
                Category = transaction.Category,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Reference = transaction.Reference,
                CreatedAtUtc = transaction.CreatedAtUtc
            };
        }

        private static BudgetTransactionDetailsDto MapToDetailsDto(BudgetTransaction transaction)
        {
            return new BudgetTransactionDetailsDto
            {
                Id = transaction.Id,
                Code = transaction.Code,
                TransactionDateUtc = transaction.TransactionDateUtc,
                TransactionType = transaction.TransactionType,
                Category = transaction.Category,
                Description = transaction.Description,
                Amount = transaction.Amount,
                Reference = transaction.Reference,
                RecordedByUserId = transaction.RecordedByUserId,
                RelatedEntityType = transaction.RelatedEntityType,
                RelatedEntityId = transaction.RelatedEntityId,
                CreatedAtUtc = transaction.CreatedAtUtc
            };
        }
    }
}
