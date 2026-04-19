using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Referrals;
using ZansiHustle.Domain.Referrals;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Referrals
{
    public class AffiliateProfileRepository : IAffiliateProfileRepository
    {
        private readonly AppDbContext _context;

        public AffiliateProfileRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<AffiliateProfile?> GetByIdAsync(Guid id)
            => _context.AffiliateProfiles.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);

        public Task<AffiliateProfile?> GetByUserIdAsync(Guid userId)
            => _context.AffiliateProfiles.Include(x => x.User).FirstOrDefaultAsync(x => x.UserId == userId);

        public Task<AffiliateProfile?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return Task.FromResult<AffiliateProfile?>(null);
            var normalised = code.Trim().ToLowerInvariant();
            return _context.AffiliateProfiles
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.ReferralCode == normalised);
        }

        public Task<bool> CodeExistsAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return Task.FromResult(false);
            var normalised = code.Trim().ToLowerInvariant();
            return _context.AffiliateProfiles.AnyAsync(x => x.ReferralCode == normalised);
        }

        public async Task AddAsync(AffiliateProfile entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _context.AffiliateProfiles.AddAsync(entity);
        }

        public void Update(AffiliateProfile entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.AffiliateProfiles.Update(entity);
        }

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
    }
}
