using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Merchants
{
    /// <summary>
    /// Repository implementation for merchant persistence operations.
    /// </summary>
    public class MerchantRepository : IMerchantRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="MerchantRepository"/> class.
        /// </summary>
        public MerchantRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<Merchant>> GetAllAsync()
        {
            return await _context.Merchants
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Merchant?> GetByIdAsync(Guid id)
        {
            return await _context.Merchants.FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<Merchant?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalizedCode = code.Trim();

            return await _context.Merchants.FirstOrDefaultAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalizedCode = code.Trim();

            return await _context.Merchants.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task AddAsync(Merchant merchant)
        {
            ArgumentNullException.ThrowIfNull(merchant);

            await _context.Merchants.AddAsync(merchant);
        }

        /// <inheritdoc />
        public void Update(Merchant merchant)
        {
            ArgumentNullException.ThrowIfNull(merchant);

            _context.Merchants.Update(merchant);
        }

        /// <inheritdoc />
        public void Delete(Merchant merchant)
        {
            ArgumentNullException.ThrowIfNull(merchant);

            _context.Merchants.Remove(merchant);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
