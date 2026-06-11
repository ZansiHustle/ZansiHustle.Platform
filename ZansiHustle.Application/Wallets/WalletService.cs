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
