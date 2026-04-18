using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Fundraising;
using ZansiHustle.Domain.Fundraising;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Fundraising;

namespace ZansiHustle.Infrastructure.Persistence.Fundraising
{
    public class StakeholderRepository : IStakeholderRepository
    {
        private readonly AppDbContext _context;

        public StakeholderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Stakeholder?> GetByIdAsync(Guid id)
        {
            return await _context.FundraisingStakeholders.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<Stakeholder>> GetAllAsync(StakeholderType? type = null, bool activeOnly = false)
        {
            var query = _context.FundraisingStakeholders.AsNoTracking();

            if (type.HasValue)
                query = query.Where(x => x.Type == type.Value);

            if (activeOnly)
                query = query.Where(x => x.IsActive);

            return await query
                .OrderBy(x => x.Type)
                .ThenByDescending(x => x.PercentageOwned)
                .ToListAsync();
        }

        public async Task AddAsync(Stakeholder stakeholder)
        {
            ArgumentNullException.ThrowIfNull(stakeholder);
            await _context.FundraisingStakeholders.AddAsync(stakeholder);
        }

        public void Update(Stakeholder stakeholder)
        {
            ArgumentNullException.ThrowIfNull(stakeholder);
            _context.FundraisingStakeholders.Update(stakeholder);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
