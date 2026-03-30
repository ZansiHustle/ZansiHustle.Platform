using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.BudgetTransactions;
using ZansiHustle.Domain.BudgetTransactions;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.BudgetTransactions
{
    /// <summary>
    /// Repository implementation for budget transaction persistence operations.
    /// </summary>
    public class BudgetTransactionRepository : IBudgetTransactionRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="BudgetTransactionRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public BudgetTransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<BudgetTransaction>> GetAllAsync()
        {
            return await _context.BudgetTransactions
                .AsNoTracking()
                .OrderByDescending(x => x.TransactionDateUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<BudgetTransaction?> GetByIdAsync(Guid id)
        {
            return await _context.BudgetTransactions.FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<BudgetTransaction?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalizedCode = code.Trim();

            return await _context.BudgetTransactions.FirstOrDefaultAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalizedCode = code.Trim();

            return await _context.BudgetTransactions.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task AddAsync(BudgetTransaction budgetTransaction)
        {
            ArgumentNullException.ThrowIfNull(budgetTransaction);

            await _context.BudgetTransactions.AddAsync(budgetTransaction);
        }

        /// <inheritdoc />
        public void Update(BudgetTransaction budgetTransaction)
        {
            ArgumentNullException.ThrowIfNull(budgetTransaction);

            _context.BudgetTransactions.Update(budgetTransaction);
        }

        /// <inheritdoc />
        public void Delete(BudgetTransaction budgetTransaction)
        {
            ArgumentNullException.ThrowIfNull(budgetTransaction);

            _context.BudgetTransactions.Remove(budgetTransaction);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
