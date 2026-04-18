using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Fundraising;
using ZansiHustle.Domain.Fundraising;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Fundraising
{
    public class ValuationRepository : IValuationRepository
    {
        private readonly AppDbContext _context;

        public ValuationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Valuation?> GetByIdAsync(Guid id)
        {
            return await _context.FundraisingValuations.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Valuation?> GetActiveAsync()
        {
            return await _context.FundraisingValuations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive);
        }

        public async Task<List<Valuation>> GetAllAsync()
        {
            return await _context.FundraisingValuations
                .AsNoTracking()
                .OrderByDescending(x => x.EffectiveFromUtc)
                .ToListAsync();
        }

        public async Task AddAsync(Valuation valuation)
        {
            ArgumentNullException.ThrowIfNull(valuation);
            await _context.FundraisingValuations.AddAsync(valuation);
        }

        public void Update(Valuation valuation)
        {
            ArgumentNullException.ThrowIfNull(valuation);
            _context.FundraisingValuations.Update(valuation);
        }

        public void Delete(Valuation valuation)
        {
            ArgumentNullException.ThrowIfNull(valuation);
            _context.FundraisingValuations.Remove(valuation);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
