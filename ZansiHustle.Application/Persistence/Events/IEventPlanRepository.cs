using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Events;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Application.Persistence.Events
{
    /// <summary>
    /// Persistence contract for the Event Builder feature. Plans are user-owned;
    /// templates are read-mostly reference data seeded at startup.
    /// </summary>
    public interface IEventPlanRepository
    {
        // Plans
        Task<EventPlan?> GetByIdAsync(Guid id);
        Task<List<EventPlan>> GetByUserAsync(Guid userId);
        Task<bool> ExistsByCodeAsync(string code);
        Task AddAsync(EventPlan plan);
        void Update(EventPlan plan);

        // Plan items
        Task<EventPlanItem?> GetItemByIdAsync(Guid itemId);
        Task AddItemAsync(EventPlanItem item);
        void UpdateItem(EventPlanItem item);
        void RemoveItem(EventPlanItem item);

        // Templates
        Task<List<EventTypeTemplate>> GetTemplateAsync(EventType eventType);

        Task<bool> SaveChangesAsync();
    }
}
