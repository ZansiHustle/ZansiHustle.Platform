using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Merchants;

namespace ZansiHustle.Application.Persistence.Merchants
{
    /// <summary>
    /// Repository contract for merchant persistence operations.
    /// </summary>
    public interface IMerchantRepository
    {
        Task<List<Merchant>> GetAllAsync();
        Task<List<Merchant>> GetByOwnerAsync(Guid ownerUserId);
        Task<Merchant?> GetByIdAsync(Guid id);
        Task<Merchant?> GetByCodeAsync(string code);
        Task<Merchant?> GetBySlugAsync(string slug);
        Task<bool> ExistsByCodeAsync(string code);
        Task<bool> ExistsBySlugAsync(string slug);
        Task AddAsync(Merchant merchant);
        void Update(Merchant merchant);
        void Delete(Merchant merchant);
        Task<bool> SaveChangesAsync();
    }
}
