using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Campaigns;
using ZansiHustle.Domain.Campaigns;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Campaigns
{
    /// <summary>
    /// Repository implementation for campaign persistence operations.
    /// </summary>
    public class CampaignRepository : ICampaignRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="CampaignRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public CampaignRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<Campaign>> GetAllAsync()
        {
            return await _context.Campaigns
                .AsNoTracking()
                .Include(x => x.ContentTasks)
                .Include(x => x.MetricSnapshots)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Campaign?> GetByIdAsync(Guid id)
        {
            return await _context.Campaigns
                .Include(x => x.ContentTasks)
                .Include(x => x.MetricSnapshots)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<Campaign?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalizedCode = code.Trim();

            return await _context.Campaigns
                .Include(x => x.ContentTasks)
                .Include(x => x.MetricSnapshots)
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

            return await _context.Campaigns.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task AddAsync(Campaign campaign)
        {
            ArgumentNullException.ThrowIfNull(campaign);

            await _context.Campaigns.AddAsync(campaign);
        }

        /// <inheritdoc />
        public void Update(Campaign campaign)
        {
            ArgumentNullException.ThrowIfNull(campaign);

            _context.Campaigns.Update(campaign);
        }

        /// <inheritdoc />
        public void Delete(Campaign campaign)
        {
            ArgumentNullException.ThrowIfNull(campaign);

            _context.Campaigns.Remove(campaign);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
