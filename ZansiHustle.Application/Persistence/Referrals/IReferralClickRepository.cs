using System.Threading.Tasks;
using ZansiHustle.Domain.Referrals;

namespace ZansiHustle.Application.Persistence.Referrals
{
    public interface IReferralClickRepository
    {
        Task AddAsync(ReferralClick entity);
        Task<bool> SaveChangesAsync();
    }
}
