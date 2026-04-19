using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Referrals;
using ZansiHustle.Domain.Referrals;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Referrals;

namespace ZansiHustle.Infrastructure.Persistence.Referrals
{
    public class UserReferralRepository : IUserReferralRepository
    {
        private readonly AppDbContext _context;

        public UserReferralRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<UserReferral?> GetByReferredUserAsync(Guid referredUserId, ReferralType type)
            => _context.UserReferrals.FirstOrDefaultAsync(
                x => x.ReferredUserId == referredUserId && x.ReferralType == type);

        public async Task AddAsync(UserReferral entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _context.UserReferrals.AddAsync(entity);
        }

        public void Update(UserReferral entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.UserReferrals.Update(entity);
        }

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
    }
}
