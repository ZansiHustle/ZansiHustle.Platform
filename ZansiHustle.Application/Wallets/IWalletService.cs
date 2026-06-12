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

        /// <summary>
        /// Create a manual withdrawal request (V1). Validates the amount against
        /// the available balance, HOLDS the amount (a WithdrawalRequested debit
        /// reduces AvailableBalance so it can't be withdrawn twice), stores only
        /// the last 4 account digits, and guards against rapid duplicate taps.
        /// Returns the masked request. No automated payout.
        /// </summary>
        Task<Result<WithdrawalRequestDto>> RequestWithdrawalAsync(Guid userId, CreateWithdrawalRequestDto request);

        /// <summary>The user's withdrawal requests, newest first (account masked).</summary>
        Task<Result<System.Collections.Generic.List<WithdrawalRequestDto>>> GetWithdrawalsAsync(Guid userId, int take = 50);

        /// <summary>
        /// Apply wallet balance toward an order at payment time (server-authoritative).
        /// Computes appliedWalletAmount = min(requested, availableBalance, orderTotal),
        /// HOLDS it via an idempotent WalletPaymentDebit (ref Order:orderId) and
        /// returns the applied amount + the remaining external amount due. Idempotent
        /// per order — a repeated call reuses the existing (net) debit and NEVER
        /// debits twice. Returns (0, orderTotal) when wallet isn't used / nothing
        /// applies. Never spends more than the available balance or the order total,
        /// and never uses pending-withdrawal money (that already left AvailableBalance).
        /// </summary>
        Task<(decimal appliedWalletAmount, decimal externalAmountDue)> ApplyToOrderAsync(
            Guid userId, Guid orderId, decimal orderTotal, string currency,
            bool useWallet, decimal? requestedAmount);

        /// <summary>
        /// Reverse a wallet payment hold for an order (credit WalletPaymentReversal,
        /// restoring AvailableBalance) when the external payment failed/cancelled.
        /// Idempotent — a second call is a no-op. Returns the reversed amount (0 when
        /// there was nothing to reverse).
        /// </summary>
        Task<decimal> ReverseOrderPaymentDebitAsync(Guid userId, Guid orderId);
    }
}
