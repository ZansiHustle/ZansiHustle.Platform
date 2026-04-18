using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Fundraising;
using ZansiHustle.Shared.Enums.Fundraising;

namespace ZansiHustle.Application.Persistence.Fundraising
{
    public interface IStakeholderRepository
    {
        Task<Stakeholder?> GetByIdAsync(Guid id);
        Task<List<Stakeholder>> GetAllAsync(StakeholderType? type = null, bool activeOnly = false);
        Task AddAsync(Stakeholder stakeholder);
        void Update(Stakeholder stakeholder);
        Task<bool> SaveChangesAsync();
    }
}
