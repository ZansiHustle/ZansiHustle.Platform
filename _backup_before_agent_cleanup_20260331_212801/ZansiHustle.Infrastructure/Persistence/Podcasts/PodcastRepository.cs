using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Podcasts;
using ZansiHustle.Domain.Podcasts;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Podcasts
{
    /// <summary>
    /// Repository implementation for podcast persistence operations.
    /// </summary>
    public class PodcastRepository : IPodcastRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="PodcastRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public PodcastRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<Podcast>> GetAllAsync()
        {
            return await _context.Podcasts
                .AsNoTracking()
                .Include(x => x.AdFormats)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Podcast?> GetByIdAsync(Guid id)
        {
            return await _context.Podcasts
                .Include(x => x.AdFormats)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<Podcast?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalizedCode = code.Trim();

            return await _context.Podcasts
                .Include(x => x.AdFormats)
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

            return await _context.Podcasts.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task AddAsync(Podcast podcast)
        {
            ArgumentNullException.ThrowIfNull(podcast);

            await _context.Podcasts.AddAsync(podcast);
        }

        /// <inheritdoc />
        public void Update(Podcast podcast)
        {
            ArgumentNullException.ThrowIfNull(podcast);

            _context.Podcasts.Update(podcast);
        }

        /// <inheritdoc />
        public void Delete(Podcast podcast)
        {
            ArgumentNullException.ThrowIfNull(podcast);

            _context.Podcasts.Remove(podcast);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
