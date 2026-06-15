using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Wallets;
using ZansiHustle.Domain.Wallets;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Wallets;

namespace ZansiHustle.Infrastructure.Persistence.Wallets
{
    public class WalletRepository : IWalletRepository
    {
        private readonly AppDbContext _context;

        public WalletRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Wallet?> GetByUserAsync(Guid userId)
        {
            return await _context.Set<Wallet>().FirstOrDefaultAsync(w => w.UserId == userId);
        }

        public async Task<List<WalletTransaction>> GetTransactionsAsync(Guid userId, int take)
        {
            return await _context.Set<WalletTransaction>()
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<WalletTransaction>> GetAllTransactionsAsync(Guid userId)
        {
            return await _context.Set<WalletTransaction>()
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<bool> HasCompletedReferenceAsync(
            WalletTransactionType type, string referenceType, Guid referenceId)
        {
            return await _context.Set<WalletTransaction>().AnyAsync(t =>
                t.Type == type
                && t.ReferenceType == referenceType
                && t.ReferenceId == referenceId
                && t.Status == WalletTransactionStatus.Completed);
        }

        public async Task<WalletTransaction?> GetCompletedReferenceAsync(
            WalletTransactionType type, string referenceType, Guid referenceId)
        {
            return await _context.Set<WalletTransaction>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t =>
                    t.Type == type
                    && t.ReferenceType == referenceType
                    && t.ReferenceId == referenceId
                    && t.Status == WalletTransactionStatus.Completed);
        }

        public async Task<decimal> GetNetWalletPaymentHeldForOrderAsync(Guid orderId)
        {
            var debits = await _context.Set<WalletTransaction>()
                .Where(t => t.Type == WalletTransactionType.WalletPaymentDebit
                            && t.ReferenceType == "Order"
                            && t.ReferenceId == orderId
                            && t.Status == WalletTransactionStatus.Completed)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            var reversals = await _context.Set<WalletTransaction>()
                .Where(t => t.Type == WalletTransactionType.WalletPaymentReversal
                            && t.ReferenceType == "Order"
                            && t.ReferenceId == orderId
                            && t.Status == WalletTransactionStatus.Completed)
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            return debits - reversals;
        }

        public async Task<T> ExecuteInOrderLockedTransactionAsync<T>(Guid orderId, Func<Task<T>> action)
        {
            ArgumentNullException.ThrowIfNull(action);

            // Execution strategy so this composes with EnableRetryOnFailure (if on)
            // — the action is idempotent (net-held reuse), so a transient retry is safe.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database
                    .BeginTransactionAsync(IsolationLevel.ReadCommitted);

                // Exclusive, transaction-scoped application lock keyed by the order.
                // Auto-released on commit/rollback. A second concurrent caller for the
                // same order BLOCKS here until the first commits, then proceeds and
                // reads the just-committed net → reuses (apply) or no-ops (reverse).
                // Only this order's wallet-payment ops serialize; everything else is
                // unaffected. Generous timeout — the critical section is sub-millisecond.
                var resource = $"wallet-order-{orderId:N}";
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC sp_getapplock @Resource = {0}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000",
                    resource);

                var result = await action();
                await tx.CommitAsync();
                return result;
            });
        }

        public async Task AddWalletAsync(Wallet wallet)
        {
            ArgumentNullException.ThrowIfNull(wallet);
            await _context.Set<Wallet>().AddAsync(wallet);
        }

        public void UpdateWallet(Wallet wallet)
        {
            ArgumentNullException.ThrowIfNull(wallet);
            _context.Set<Wallet>().Update(wallet);
        }

        public async Task AddTransactionAsync(WalletTransaction transaction)
        {
            ArgumentNullException.ThrowIfNull(transaction);
            await _context.Set<WalletTransaction>().AddAsync(transaction);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task AddWithdrawalAsync(WalletWithdrawalRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _context.Set<WalletWithdrawalRequest>().AddAsync(request);
        }

        public async Task<List<WalletWithdrawalRequest>> GetWithdrawalsByUserAsync(Guid userId, int take)
        {
            return await _context.Set<WalletWithdrawalRequest>()
                .AsNoTracking()
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.RequestedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<bool> HasRecentPendingWithdrawalAsync(Guid userId, int withinSeconds)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-withinSeconds);
            return await _context.Set<WalletWithdrawalRequest>().AnyAsync(w =>
                w.UserId == userId
                && w.Status == WithdrawalRequestStatus.Pending
                && w.RequestedAtUtc >= cutoff);
        }
    }
}
