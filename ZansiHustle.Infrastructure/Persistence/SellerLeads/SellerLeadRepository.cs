using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.SellerLeads;
using ZansiHustle.Domain.SellerLeads;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.SellerLeads
{
    /// <summary>
    /// Repository implementation for seller lead persistence operations.
    /// </summary>
    public class SellerLeadRepository : ISellerLeadRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="SellerLeadRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public SellerLeadRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<SellerLead>> GetAllAsync()
        {
            return await _context.SellerLeads
                .AsNoTracking()
                .Include(x => x.AssignedUser)
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<SellerLead>> GetByUserIdAsync(Guid userId)
        {
            return await _context.SellerLeads
                .AsNoTracking()
                .Include(x => x.AssignedUser)
                .Where(x => x.AssignedUserId == userId)
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<SellerLead?> FindFallbackForMerchantAsync(Guid merchantId, string? email)
        {
            // Strongest signal: the admin conversion flow stamped this lead's
            // ConvertedSellerId with the merchant it produced. Prefer that.
            var byConverted = await _context.SellerLeads
                .AsNoTracking()
                .Where(x => x.ConvertedSellerId == merchantId)
                .OrderByDescending(x => x.SubmittedAtUtc)
                .FirstOrDefaultAsync();
            if (byConverted != null) return byConverted;

            // Fallback: any lead the same user submitted (identified by
            // email). Fuzzy but covers the legacy case where a lead was
            // never formally "converted" but the user later created a
            // Merchant directly under the same email.
            if (string.IsNullOrWhiteSpace(email)) return null;

            return await _context.SellerLeads
                .AsNoTracking()
                .Where(x => x.Email != null && x.Email.ToLower() == email.ToLower())
                .OrderByDescending(x => x.SubmittedAtUtc)
                .FirstOrDefaultAsync();
        }

        /// <inheritdoc />
        public async Task<SellerLead?> GetByIdAsync(Guid id)
        {
            return await _context.SellerLeads
                .Include(x => x.AssignedUser)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<SellerLead?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalizedCode = code.Trim();

            return await _context.SellerLeads
                .Include(x => x.AssignedUser)
                .FirstOrDefaultAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalizedCode = code.Trim();

            return await _context.SellerLeads.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task AddAsync(SellerLead sellerLead)
        {
            ArgumentNullException.ThrowIfNull(sellerLead);

            await _context.SellerLeads.AddAsync(sellerLead);
        }

        /// <inheritdoc />
        public void Update(SellerLead sellerLead)
        {
            ArgumentNullException.ThrowIfNull(sellerLead);

            _context.SellerLeads.Update(sellerLead);
        }

        /// <inheritdoc />
        public void Delete(SellerLead sellerLead)
        {
            ArgumentNullException.ThrowIfNull(sellerLead);

            _context.SellerLeads.Remove(sellerLead);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}

