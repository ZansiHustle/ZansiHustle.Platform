using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Wallets;
using ZansiHustle.Shared.Enums.Wallets;

namespace ZansiHustle.Application.Persistence.Wallets
{
    public interface IWalletRepository
    {
        /// <summary>The user's wallet (tracked) — null when not created yet.</summary>
        Task<Wallet?> GetByUserAsync(Guid userId);

        /// <summary>Newest-first ledger entries for a user, capped at <paramref name="take"/>.</summary>
        Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int take);

        /// <summary>
        /// True when a Completed transaction of this type already references the
        /// given (referenceType, referenceId) — the idempotency guard that stops
        /// a repeated trigger (e.g. reject called twice) double-crediting.
        /// </summary>
        Task<bool> HasCompletedReferenceAsync(
            WalletTransactionType type, string referenceType, Guid referenceId);

        Task AddWalletAsync(Wallet wallet);
        void UpdateWallet(Wallet wallet);
        Task AddTransactionAsync(WalletTransaction transaction);
        Task<bool> SaveChangesAsync();
    }
}
