using System;
using System.Collections.Generic;
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

        public async Task<bool> HasCompletedReferenceAsync(
            WalletTransactionType type, string referenceType, Guid referenceId)
        {
            return await _context.Set<WalletTransaction>().AnyAsync(t =>
                t.Type == type
                && t.ReferenceType == referenceType
                && t.ReferenceId == referenceId
                && t.Status == WalletTransactionStatus.Completed);
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
    }
}
