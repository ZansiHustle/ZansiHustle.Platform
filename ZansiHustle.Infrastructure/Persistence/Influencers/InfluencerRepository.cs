using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Influencers;
using ZansiHustle.Domain.Influencers;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Influencers
{
    /// <summary>
    /// Repository implementation for influencer persistence operations.
    /// </summary>
    public class InfluencerRepository : IInfluencerRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="InfluencerRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public InfluencerRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<Influencer>> GetAllAsync()
        {
            return await _context.Influencers
                .AsNoTracking()
                .Include(x => x.PlatformAccounts)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Influencer?> GetByIdAsync(Guid id)
        {
            return await _context.Influencers
                .Include(x => x.PlatformAccounts)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<Influencer?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalizedCode = code.Trim();

            return await _context.Influencers
                .Include(x => x.PlatformAccounts)
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

            return await _context.Influencers.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task AddAsync(Influencer influencer)
        {
            ArgumentNullException.ThrowIfNull(influencer);

            await _context.Influencers.AddAsync(influencer);
        }

        /// <inheritdoc />
        public void Update(Influencer influencer)
        {
            ArgumentNullException.ThrowIfNull(influencer);

            _context.Influencers.Update(influencer);
        }

        /// <inheritdoc />
        public void Delete(Influencer influencer)
        {
            ArgumentNullException.ThrowIfNull(influencer);

            _context.Influencers.Remove(influencer);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
