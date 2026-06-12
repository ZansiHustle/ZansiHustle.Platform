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

        /// <summary>The Completed transaction of this type referencing
        /// (referenceType, referenceId), or null. Used to read the held wallet
        /// amount for an order (reuse on retry) and to compute reversal amounts.</summary>
        Task<WalletTransaction?> GetCompletedReferenceAsync(
            WalletTransactionType type, string referenceType, Guid referenceId);

        /// <summary>
        /// Net wallet amount currently HELD against an order =
        /// Σ(WalletPaymentDebit) − Σ(WalletPaymentReversal) (Completed only).
        /// Drives the apply/reverse idempotency: net &gt; 0 → already applied
        /// (reuse, no re-debit); net ≤ 0 → free to apply (or reversal is a no-op).
        /// Supports the cancel → re-apply cycle for split payments.
        /// </summary>
        Task<decimal> GetNetWalletPaymentHeldForOrderAsync(Guid orderId);

        /// <summary>
        /// Run <paramref name="action"/> inside a DB transaction holding an
        /// EXCLUSIVE application lock keyed by the order, so concurrent
        /// wallet-payment apply/reverse calls for the SAME order are fully
        /// serialized — the second caller blocks until the first commits, then
        /// reads the just-committed net (reuse / no-op). Closes the simultaneous
        /// "both read net=0 then both debit" window at the database level. Only
        /// this order's wallet-payment ops serialize — refunds, withdrawals and
        /// other orders are unaffected. Uses the context execution strategy so it
        /// composes with retry-on-failure.
        /// </summary>
        Task<T> ExecuteInOrderLockedTransactionAsync<T>(Guid orderId, Func<Task<T>> action);

        Task AddWalletAsync(Wallet wallet);
        void UpdateWallet(Wallet wallet);
        Task AddTransactionAsync(WalletTransaction transaction);
        Task<bool> SaveChangesAsync();

        // ── Withdrawals ────────────────────────────────────────────────────────
        Task AddWithdrawalAsync(WalletWithdrawalRequest request);

        /// <summary>The user's withdrawal requests, newest first.</summary>
        Task<List<WalletWithdrawalRequest>> GetWithdrawalsByUserAsync(Guid userId, int take);

        /// <summary>True when the user already has a Pending withdrawal created
        /// within <paramref name="withinSeconds"/> — the rapid double-tap guard.</summary>
        Task<bool> HasRecentPendingWithdrawalAsync(Guid userId, int withinSeconds);
    }
}
