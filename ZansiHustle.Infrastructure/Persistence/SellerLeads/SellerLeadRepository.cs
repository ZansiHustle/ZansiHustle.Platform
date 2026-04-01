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

