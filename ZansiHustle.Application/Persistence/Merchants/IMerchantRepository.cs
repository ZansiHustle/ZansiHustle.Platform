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
        Task<Merchant?> GetByIdAsync(Guid id);
        Task<Merchant?> GetByCodeAsync(string code);
        Task<bool> ExistsByCodeAsync(string code);
        Task AddAsync(Merchant merchant);
        void Update(Merchant merchant);
        void Delete(Merchant merchant);
        Task<bool> SaveChangesAsync();
    }
}
