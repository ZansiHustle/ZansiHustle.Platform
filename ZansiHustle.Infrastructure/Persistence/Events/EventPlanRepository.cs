using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Events;
using ZansiHustle.Domain.Events;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Infrastructure.Persistence.Events
{
    public class EventPlanRepository : IEventPlanRepository
    {
        private readonly AppDbContext _context;

        public EventPlanRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<EventPlan?> GetByIdAsync(Guid id)
        {
            return await _context.EventPlans
                .Include(x => x.Items)
                    .ThenInclude(i => i.Listing)
                        .ThenInclude(l => l!.Merchant)
                .Include(x => x.Items)
                    .ThenInclude(i => i.SellerSubcategory)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task<List<EventPlan>> GetByUserAsync(Guid userId)
        {
            return await _context.EventPlans
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x => x.UserId == userId && x.Status != EventPlanStatus.Archived)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var normalized = code.Trim();
            return await _context.EventPlans.AnyAsync(x => x.Code == normalized);
        }

        /// <inheritdoc />
        public async Task AddAsync(EventPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);
            await _context.EventPlans.AddAsync(plan);
        }

        /// <inheritdoc />
        public void Update(EventPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);
            _context.EventPlans.Update(plan);
        }

        /// <inheritdoc />
        public async Task<EventPlanItem?> GetItemByIdAsync(Guid itemId)
        {
            return await _context.EventPlanItems
                .Include(x => x.EventPlan)
                .Include(x => x.Listing)
                    .ThenInclude(l => l!.Merchant)
                .Include(x => x.SellerSubcategory)
                .FirstOrDefaultAsync(x => x.Id == itemId);
        }

        /// <inheritdoc />
        public async Task AddItemAsync(EventPlanItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            await _context.EventPlanItems.AddAsync(item);
        }

        /// <inheritdoc />
        public void UpdateItem(EventPlanItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _context.EventPlanItems.Update(item);
        }

        /// <inheritdoc />
        public void RemoveItem(EventPlanItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            _context.EventPlanItems.Remove(item);
        }

        /// <inheritdoc />
        public async Task<List<EventTypeTemplate>> GetTemplateAsync(EventType eventType)
        {
            return await _context.EventTypeTemplates
                .AsNoTracking()
                .Where(x => x.EventType == eventType && x.IsActive)
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.DisplayOrder)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
