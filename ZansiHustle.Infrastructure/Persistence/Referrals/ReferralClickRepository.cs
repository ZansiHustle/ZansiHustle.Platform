using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Persistence.Referrals;
using ZansiHustle.Domain.Referrals;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Referrals
{
    public class ReferralClickRepository : IReferralClickRepository
    {
        private readonly AppDbContext _context;

        public ReferralClickRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ReferralClick entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _context.ReferralClicks.AddAsync(entity);
        }

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
    }
}
