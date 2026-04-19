using System;
using System.Threading.Tasks;
using ZansiHustle.Domain.Referrals;
using ZansiHustle.Shared.Enums.Referrals;

namespace ZansiHustle.Application.Persistence.Referrals
{
    public interface IUserReferralRepository
    {
        Task<UserReferral?> GetByReferredUserAsync(Guid referredUserId, ReferralType type);
        Task AddAsync(UserReferral entity);
        void Update(UserReferral entity);
        Task<bool> SaveChangesAsync();
    }
}
