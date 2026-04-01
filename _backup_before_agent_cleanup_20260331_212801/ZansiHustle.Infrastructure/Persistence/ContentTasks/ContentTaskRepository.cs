using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.ContentTasks;
using ZansiHustle.Domain.ContentTasks;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.ContentTasks
{
    /// <summary>
    /// Repository implementation for content task persistence operations.
    /// </summary>
    public class ContentTaskRepository : IContentTaskRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="ContentTaskRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public ContentTaskRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<ContentTask>> GetAllAsync()
        {
            return await _context.ContentTasks
                .AsNoTracking()
                .Include(x => x.Campaign)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<ContentTask?> GetByIdAsync(Guid id)
        {
            return await _context.ContentTasks
                .Include(x => x.Campaign)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task AddAsync(ContentTask contentTask)
        {
            ArgumentNullException.ThrowIfNull(contentTask);

            await _context.ContentTasks.AddAsync(contentTask);
        }

        /// <inheritdoc />
        public void Update(ContentTask contentTask)
        {
            ArgumentNullException.ThrowIfNull(contentTask);

            _context.ContentTasks.Update(contentTask);
        }

        /// <inheritdoc />
        public void Delete(ContentTask contentTask)
        {
            ArgumentNullException.ThrowIfNull(contentTask);

            _context.ContentTasks.Remove(contentTask);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
