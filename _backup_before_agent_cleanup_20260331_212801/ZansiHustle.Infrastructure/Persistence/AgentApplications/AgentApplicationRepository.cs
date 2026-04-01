using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.AgentApplications;
using ZansiHustle.Domain.AgentApplications;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.AgentApplications
{
    /// <summary>
    /// Repository implementation for agent application persistence operations.
    /// </summary>
    public class AgentApplicationRepository : IAgentApplicationRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="AgentApplicationRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public AgentApplicationRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<AgentApplication>> GetAllAsync()
        {
            return await _context.AgentApplications
                .AsNoTracking()
                .OrderByDescending(x => x.SubmittedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<AgentApplication?> GetByIdAsync(Guid id)
        {
            return await _context.AgentApplications.FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<AgentApplication?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalizedCode = code.Trim();

            return await _context.AgentApplications.FirstOrDefaultAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalizedCode = code.Trim();

            return await _context.AgentApplications.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task AddAsync(AgentApplication application)
        {
            ArgumentNullException.ThrowIfNull(application);

            await _context.AgentApplications.AddAsync(application);
        }

        /// <inheritdoc />
        public void Update(AgentApplication application)
        {
            ArgumentNullException.ThrowIfNull(application);

            _context.AgentApplications.Update(application);
        }

        /// <inheritdoc />
        public void Delete(AgentApplication application)
        {
            ArgumentNullException.ThrowIfNull(application);

            _context.AgentApplications.Remove(application);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
