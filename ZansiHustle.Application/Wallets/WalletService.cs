using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Wallets;
using ZansiHustle.Application.Wallets.Dtos;
using ZansiHustle.Domain.Wallets;
using ZansiHustle.Shared.Enums.Wallets;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Wallets
{
    /// <inheritdoc />
    public sealed class WalletService : IWalletService
    {
        private const int MaxTake = 100;
        private const string DefaultCurrency = "ZAR";

        private readonly IWalletRepository _wallets;
        private readonly ILogger<WalletService> _logger;

        public WalletService(IWalletRepository wallets, ILogger<WalletService> logger)
        {
            _wallets = wallets;
            _logger = logger;
        }

        public async Task<Result<WalletDto>> GetWalletAsync(Guid userId)
        {
            if (userId == Guid.Empty)
                return Result<WalletDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            var wallet = await EnsureWalletAsync(userId, DefaultCurrency);
            return Result<WalletDto>.Success(new WalletDto
            {
                AvailableBalance = wallet.AvailableBalance,
                Currency = wallet.Currency
            }, "Wallet loaded.");
        }

        public async Task<Result<List<WalletTransactionDto>>> GetTransactionsAsync(Guid userId, int take = 50)
        {
            if (userId == Guid.Empty)
                return Result<List<WalletTransactionDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            take = Math.Clamp(take <= 0 ? 50 : take, 1, MaxTake);
            var rows = await _wallets.GetTransactionsAsync(userId, take);
            return Result<List<WalletTransactionDto>>.Success(rows.Select(ToDto).ToList(), "Transactions loaded.");
        }

        public async Task<WalletTransaction?> CreditAsync(
            Guid userId,
            WalletTransactionType type,
            decimal amount,
            string currency,
            string referenceType,
            Guid referenceId,
            string description)
        {
            if (userId == Guid.Empty || amount <= 0m)
                return null;

            // Idempotency: never credit the same (type, reference) twice. The DB
            // also enforces this via a filtered unique index — this is the cheap
            // pre-check that turns a duplicate into a clean no-op.
            if (referenceId != Guid.Empty &&
                await _wallets.HasCompletedReferenceAsync(type, referenceType, referenceId))
            {
                _logger.LogInformation(
                    "[Wallet] Skipped duplicate {Type} credit for {RefType}:{RefId} (user {UserId}).",
                    type, referenceType, referenceId, userId);
                return null;
            }

            var resolvedCurrency = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency;
            var wallet = await EnsureWalletAsync(userId, resolvedCurrency);

            var nowUtc = DateTime.UtcNow;
            var newBalance = wallet.AvailableBalance + amount;

            var tx = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                UserId = userId,
                Type = type,
                Direction = WalletTransactionDirection.Credit,
                Amount = amount,
                Currency = resolvedCurrency,
                BalanceAfter = newBalance,
                ReferenceType = referenceType,
                ReferenceId = referenceId == Guid.Empty ? null : referenceId,
                Description = description,
                Status = WalletTransactionStatus.Completed,
                CreatedAtUtc = nowUtc
            };

            wallet.AvailableBalance = newBalance;
            wallet.UpdatedAtUtc = nowUtc;

            await _wallets.AddTransactionAsync(tx);
            _wallets.UpdateWallet(wallet);
            await _wallets.SaveChangesAsync();

            _logger.LogInformation(
                "[Wallet] Credited {Amount} {Currency} to user {UserId} ({Type}, {RefType}:{RefId}). New balance {Balance}.",
                amount, resolvedCurrency, userId, type, referenceType, referenceId, newBalance);

            return tx;
        }

        // ── Withdrawals (manual, V1) ─────────────────────────────────────────

        private const int DuplicateGuardSeconds = 10;

        public async Task<Result<WithdrawalRequestDto>> RequestWithdrawalAsync(
            Guid userId, CreateWithdrawalRequestDto request)
        {
            if (userId == Guid.Empty)
                return Result<WithdrawalRequestDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");
            if (request is null)
                return Result<WithdrawalRequestDto>.Failure(ErrorCodes.BadRequest, "Request body is required.");

            if (request.Amount <= 0m)
                return Result<WithdrawalRequestDto>.Failure(ErrorCodes.BadRequest, "Enter an amount greater than zero.");
            if (string.IsNullOrWhiteSpace(request.BankName))
                return Result<WithdrawalRequestDto>.Failure(ErrorCodes.BadRequest, "Bank name is required.");
            if (string.IsNullOrWhiteSpace(request.AccountHolderName))
                return Result<WithdrawalRequestDto>.Failure(ErrorCodes.BadRequest, "Account holder name is required.");

            var digits = new string((request.AccountNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length < 4)
                return Result<WithdrawalRequestDto>.Failure(ErrorCodes.BadRequest, "Enter a valid account number.");

            var accountType = ParseAccountType(request.AccountType);
            if (accountType is null)
                return Result<WithdrawalRequestDto>.Failure(ErrorCodes.BadRequest, "Choose a valid account type.");

            var wallet = await EnsureWalletAsync(userId, DefaultCurrency);

            // Round to cents and re-check against the LIVE balance (the hold below
            // decrements it, so a second request can't exceed what's left).
            var amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
            if (amount > wallet.AvailableBalance)
                return Result<WithdrawalRequestDto>.Failure(
                    ErrorCodes.BadRequest, "Amount exceeds your available wallet balance.");

            // Rapid double-tap guard — collapse a burst into one request.
            if (await _wallets.HasRecentPendingWithdrawalAsync(userId, DuplicateGuardSeconds))
                return Result<WithdrawalRequestDto>.Failure(
                    ErrorCodes.Conflict, "You already have a withdrawal request being submitted. Please wait a moment.");

            var nowUtc = DateTime.UtcNow;
            var last4 = digits[^4..];

            var withdrawal = new WalletWithdrawalRequest
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                UserId = userId,
                Amount = amount,
                Currency = wallet.Currency,
                BankName = request.BankName.Trim(),
                AccountHolderName = request.AccountHolderName.Trim(),
                AccountNumberLast4 = last4, // full number is NEVER stored
                BranchCode = string.IsNullOrWhiteSpace(request.BranchCode) ? null : request.BranchCode.Trim(),
                AccountType = accountType.Value,
                Status = WithdrawalRequestStatus.Pending,
                Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                RequestedAtUtc = nowUtc
            };

            // HOLD the funds: a Completed debit reduces the available balance so the
            // same money can't be requested again. Admin reject/cancel later credits
            // it back. ReferenceId = the withdrawal id (idempotency anchor).
            var newBalance = wallet.AvailableBalance - amount;
            var debit = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                UserId = userId,
                Type = WalletTransactionType.WithdrawalRequested,
                Direction = WalletTransactionDirection.Debit,
                Amount = amount,
                Currency = wallet.Currency,
                BalanceAfter = newBalance,
                ReferenceType = "WalletWithdrawalRequest",
                ReferenceId = withdrawal.Id,
                Description = "Withdrawal requested",
                Status = WalletTransactionStatus.Completed,
                CreatedAtUtc = nowUtc
            };

            wallet.AvailableBalance = newBalance;
            wallet.UpdatedAtUtc = nowUtc;

            await _wallets.AddWithdrawalAsync(withdrawal);
            await _wallets.AddTransactionAsync(debit);
            _wallets.UpdateWallet(wallet);
            await _wallets.SaveChangesAsync();

            _logger.LogInformation(
                "[Wallet] Withdrawal requested {Amount} {Currency} by user {UserId} (request {RequestId}). New balance {Balance}.",
                amount, wallet.Currency, userId, withdrawal.Id, newBalance);

            return Result<WithdrawalRequestDto>.Success(ToWithdrawalDto(withdrawal), "Withdrawal requested.");
        }

        public async Task<Result<List<WithdrawalRequestDto>>> GetWithdrawalsAsync(Guid userId, int take = 50)
        {
            if (userId == Guid.Empty)
                return Result<List<WithdrawalRequestDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            take = Math.Clamp(take <= 0 ? 50 : take, 1, MaxTake);
            var rows = await _wallets.GetWithdrawalsByUserAsync(userId, take);
            return Result<List<WithdrawalRequestDto>>.Success(rows.Select(ToWithdrawalDto).ToList(), "Withdrawals loaded.");
        }

        private static WithdrawalAccountType? ParseAccountType(string? raw)
        {
            var v = (raw ?? string.Empty).Trim();
            if (string.Equals(v, "Cheque", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "Current", StringComparison.OrdinalIgnoreCase))
                return WithdrawalAccountType.Cheque;
            if (string.Equals(v, "Savings", StringComparison.OrdinalIgnoreCase))
                return WithdrawalAccountType.Savings;
            if (string.Equals(v, "Business", StringComparison.OrdinalIgnoreCase))
                return WithdrawalAccountType.Business;
            return null;
        }

        private static WithdrawalRequestDto ToWithdrawalDto(WalletWithdrawalRequest w) => new()
        {
            Id = w.Id,
            Amount = w.Amount,
            Currency = w.Currency,
            BankName = w.BankName,
            AccountHolderName = w.AccountHolderName,
            AccountNumberMasked = "****" + w.AccountNumberLast4,
            BranchCode = w.BranchCode,
            AccountType = w.AccountType.ToString(),
            Status = w.Status.ToString(),
            Note = w.Note,
            AdminNote = w.AdminNote,
            RequestedAtUtc = w.RequestedAtUtc,
            ProcessedAtUtc = w.ProcessedAtUtc
        };

        // ── Wallet-as-payment (checkout) ─────────────────────────────────────

        public async Task<(decimal appliedWalletAmount, decimal externalAmountDue)> ApplyToOrderAsync(
            Guid userId, Guid orderId, decimal orderTotal, string currency,
            bool useWallet, decimal? requestedAmount)
        {
            if (userId == Guid.Empty || orderId == Guid.Empty || orderTotal <= 0m)
                return (0m, orderTotal);

            // Serialize all wallet-payment apply/reverse for THIS order at the DB
            // level (sp_getapplock) so two simultaneous requests can't both read
            // net=0 and each create a debit. The second caller blocks until the
            // first commits, then reads the committed net and reuses it.
            return await _wallets.ExecuteInOrderLockedTransactionAsync(orderId, async () =>
            {
            // Idempotency / reuse via NET held (Σdebits − Σreversals). If wallet is
            // already held for this order (net > 0) we REUSE it — a double-tap during
            // an active attempt collapses to one debit. After a reversal (cancelled
            // split) net == 0, so a retry is free to apply wallet AGAIN based on the
            // current balance — the apply → reverse → re-apply cycle the product needs.
            var netHeld = await _wallets.GetNetWalletPaymentHeldForOrderAsync(orderId);
            if (netHeld > 0m)
                return (netHeld, orderTotal - netHeld);

            if (!useWallet) return (0m, orderTotal);

            var wallet = await EnsureWalletAsync(userId, currency);
            var available = wallet.AvailableBalance;
            if (available <= 0m) return (0m, orderTotal);

            // Server decides the applied amount. "Use maximum" = no positive
            // request → min(available, total). Otherwise clamp the request too.
            var desired = requestedAmount is > 0m ? requestedAmount!.Value : available;
            var applied = Math.Min(Math.Min(desired, available), orderTotal);
            applied = Math.Round(applied, 2, MidpointRounding.AwayFromZero);
            if (applied <= 0m) return (0m, orderTotal);

            var nowUtc = DateTime.UtcNow;
            var newBalance = available - applied;
            var debit = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                UserId = userId,
                Type = WalletTransactionType.WalletPaymentDebit,
                Direction = WalletTransactionDirection.Debit,
                Amount = applied,
                Currency = wallet.Currency,
                BalanceAfter = newBalance,
                ReferenceType = "Order",
                ReferenceId = orderId,
                Description = "Wallet payment for order",
                Status = WalletTransactionStatus.Completed,
                CreatedAtUtc = nowUtc
            };

            wallet.AvailableBalance = newBalance;
            wallet.UpdatedAtUtc = nowUtc;

            await _wallets.AddTransactionAsync(debit);
            _wallets.UpdateWallet(wallet);
            await _wallets.SaveChangesAsync();

            _logger.LogInformation(
                "[Wallet] Applied {Amount} {Currency} to order {OrderId} for user {UserId}. New balance {Balance}.",
                applied, wallet.Currency, orderId, userId, newBalance);

            return (applied, orderTotal - applied);
            });
        }

        public async Task<decimal> ReverseOrderPaymentDebitAsync(Guid userId, Guid orderId)
        {
            if (userId == Guid.Empty || orderId == Guid.Empty) return 0m;

            // Serialize with any concurrent apply/reverse for this order (same
            // sp_getapplock key) so two simultaneous reversals can't both credit.
            return await _wallets.ExecuteInOrderLockedTransactionAsync(orderId, async () =>
            {
            // Reverse exactly the amount CURRENTLY held (net of any prior reversal).
            // When net ≤ 0 there's nothing to restore → clean no-op, so webhook +
            // cancel retries can't over-credit. NOT idempotent-by-reference (the
            // unique guard excludes type 101) — the net-held check is the guard, so
            // an apply → reverse → re-apply → reverse cycle reverses correctly each time.
            var netHeld = await _wallets.GetNetWalletPaymentHeldForOrderAsync(orderId);
            if (netHeld <= 0m)
            {
                _logger.LogInformation(
                    "[Wallet] Reversal for order {OrderId} skipped (nothing held).", orderId);
                return 0m;
            }

            var wallet = await EnsureWalletAsync(userId, DefaultCurrency);
            var nowUtc = DateTime.UtcNow;
            var newBalance = wallet.AvailableBalance + netHeld;

            var reversal = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                UserId = userId,
                Type = WalletTransactionType.WalletPaymentReversal,
                Direction = WalletTransactionDirection.Credit,
                Amount = netHeld,
                Currency = wallet.Currency,
                BalanceAfter = newBalance,
                ReferenceType = "Order",
                ReferenceId = orderId,
                Description = "Wallet payment restored",
                Status = WalletTransactionStatus.Completed,
                CreatedAtUtc = nowUtc
            };

            wallet.AvailableBalance = newBalance;
            wallet.UpdatedAtUtc = nowUtc;

            await _wallets.AddTransactionAsync(reversal);
            _wallets.UpdateWallet(wallet);
            await _wallets.SaveChangesAsync();

            _logger.LogInformation(
                "[Wallet] Reversed {Amount} {Currency} for order {OrderId} (user {UserId}). New balance {Balance}.",
                netHeld, wallet.Currency, orderId, userId, newBalance);
            return netHeld;
            });
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private async Task<Wallet> EnsureWalletAsync(Guid userId, string currency)
        {
            var existing = await _wallets.GetByUserAsync(userId);
            if (existing is not null) return existing;

            var nowUtc = DateTime.UtcNow;
            var wallet = new Wallet
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Currency = string.IsNullOrWhiteSpace(currency) ? DefaultCurrency : currency,
                AvailableBalance = 0m,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            };
            await _wallets.AddWalletAsync(wallet);
            await _wallets.SaveChangesAsync();
            return wallet;
        }

        private static WalletTransactionDto ToDto(WalletTransaction t) => new()
        {
            Id = t.Id,
            Type = t.Type.ToString(),
            Direction = t.Direction.ToString(),
            Amount = t.Amount,
            Currency = t.Currency,
            BalanceAfter = t.BalanceAfter,
            Description = t.Description,
            Status = t.Status.ToString(),
            ReferenceType = t.ReferenceType,
            ReferenceId = t.ReferenceId,
            CreatedAtUtc = t.CreatedAtUtc
        };
    }
}
