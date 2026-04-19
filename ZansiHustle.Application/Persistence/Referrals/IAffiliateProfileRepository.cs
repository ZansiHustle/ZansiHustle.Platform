using System;
using System.Threading.Tasks;
using ZansiHustle.Domain.Referrals;

namespace ZansiHustle.Application.Persistence.Referrals
{
    public interface IAffiliateProfileRepository
    {
        Task<AffiliateProfile?> GetByIdAsync(Guid id);
        Task<AffiliateProfile?> GetByUserIdAsync(Guid userId);
        /// <summary>Case-insensitive lookup by referral slug. Used to resolve /join/{slug}.</summary>
        Task<AffiliateProfile?> GetByCodeAsync(string code);
        /// <summary>True if the slug already exists. Used during slug generation.</summary>
        Task<bool> CodeExistsAsync(string code);

        Task AddAsync(AffiliateProfile entity);
        void Update(AffiliateProfile entity);

        Task<bool> SaveChangesAsync();
    }
}
