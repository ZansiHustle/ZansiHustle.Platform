using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Agents;
using ZansiHustle.Domain.Agents;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Agents
{
    /// <summary>
    /// Repository implementation for agent persistence operations.
    /// </summary>
    public class AgentRepository : IAgentRepository
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Creates a new instance of the <see cref="AgentRepository"/> class.
        /// </summary>
        /// <param name="context">The application database context.</param>
        public AgentRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<List<Agent>> GetAllAsync()
        {
            return await _context.Agents
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Agent?> GetByIdAsync(Guid id)
        {
            return await _context.Agents.FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<Agent?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var normalizedCode = code.Trim();

            return await _context.Agents.FirstOrDefaultAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            var normalizedCode = code.Trim();

            return await _context.Agents.AnyAsync(x => x.Code == normalizedCode);
        }

        /// <inheritdoc />
        public async Task AddAsync(Agent agent)
        {
            ArgumentNullException.ThrowIfNull(agent);

            await _context.Agents.AddAsync(agent);
        }

        /// <inheritdoc />
        public void Update(Agent agent)
        {
            ArgumentNullException.ThrowIfNull(agent);

            _context.Agents.Update(agent);
        }

        /// <inheritdoc />
        public void Delete(Agent agent)
        {
            ArgumentNullException.ThrowIfNull(agent);

            _context.Agents.Remove(agent);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
