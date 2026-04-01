using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.BudgetTransactions;

namespace ZansiHustle.Application.Persistence.BudgetTransactions
{
    /// <summary>
    /// Repository contract for managing budget transaction persistence operations.
    /// </summary>
    public interface IBudgetTransactionRepository
    {
        /// <summary>
        /// Gets all budget transactions in the system.
        /// </summary>
        /// <returns>A list of budget transactions.</returns>
        Task<List<BudgetTransaction>> GetAllAsync();

        /// <summary>
        /// Gets a single budget transaction by its unique identifier.
        /// </summary>
        /// <param name="id">The budget transaction identifier.</param>
        /// <returns>The matching transaction if found; otherwise null.</returns>
        Task<BudgetTransaction?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a single budget transaction by its unique business code.
        /// </summary>
        /// <param name="code">The budget transaction code.</param>
        /// <returns>The matching transaction if found; otherwise null.</returns>
        Task<BudgetTransaction?> GetByCodeAsync(string code);

        /// <summary>
        /// Checks whether a budget transaction code already exists.
        /// </summary>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code exists; otherwise false.</returns>
        Task<bool> ExistsByCodeAsync(string code);

        /// <summary>
        /// Adds a new budget transaction to the data store.
        /// </summary>
        /// <param name="budgetTransaction">The budget transaction entity to add.</param>
        Task AddAsync(BudgetTransaction budgetTransaction);

        /// <summary>
        /// Updates an existing budget transaction in the data store.
        /// </summary>
        /// <param name="budgetTransaction">The budget transaction entity to update.</param>
        void Update(BudgetTransaction budgetTransaction);

        /// <summary>
        /// Deletes a budget transaction from the data store.
        /// </summary>
        /// <param name="budgetTransaction">The budget transaction entity to delete.</param>
        void Delete(BudgetTransaction budgetTransaction);

        /// <summary>
        /// Persists all pending repository changes to the database.
        /// </summary>
        /// <returns>True if one or more records were affected; otherwise false.</returns>
        Task<bool> SaveChangesAsync();
    }
}
