using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Wallets.Dtos;
using ZansiHustle.Domain.Wallets;
using ZansiHustle.Shared.Enums.Wallets;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Wallets
{
    /// <summary>
    /// Wallet ledger. Read endpoints + a single idempotent credit primitive used
    /// by the rejection/refund flows. NO wallet-as-payment-method here yet.
    /// </summary>
    public interface IWalletService
    {
        /// <summary>Balance (creating an empty wallet on first fetch).</summary>
        Task<Result<WalletDto>> GetWalletAsync(Guid userId);

        /// <summary>Recent ledger entries, newest first.</summary>
        Task<Result<System.Collections.Generic.List<WalletTransactionDto>>> GetTransactionsAsync(
            Guid userId, int take = 50);

        /// <summary>
        /// Credit a user's wallet idempotently. If a Completed transaction of the
        /// same <paramref name="type"/> already references (referenceType,
        /// referenceId), the existing entry is returned and NO new credit is
        /// made. Balance is updated atomically with the ledger entry. A
        /// non-positive amount is a no-op (returns failure). Persists on the
        /// shared DbContext.
        /// </summary>
        Task<WalletTransaction?> CreditAsync(
            Guid userId,
            WalletTransactionType type,
            decimal amount,
            string currency,
            string referenceType,
            Guid referenceId,
            string description);
    }
}
