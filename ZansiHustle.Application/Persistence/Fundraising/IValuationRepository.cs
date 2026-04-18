using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Fundraising;

namespace ZansiHustle.Application.Persistence.Fundraising
{
    public interface IValuationRepository
    {
        Task<Valuation?> GetByIdAsync(Guid id);
        Task<Valuation?> GetActiveAsync();
        Task<List<Valuation>> GetAllAsync();
        Task AddAsync(Valuation valuation);
        void Update(Valuation valuation);
        void Delete(Valuation valuation);
        Task<bool> SaveChangesAsync();
    }
}
