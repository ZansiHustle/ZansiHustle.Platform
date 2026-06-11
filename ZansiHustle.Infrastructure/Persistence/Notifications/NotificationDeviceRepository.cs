using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Notifications;
using ZansiHustle.Domain.Notifications;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Notifications;

namespace ZansiHustle.Infrastructure.Persistence.Notifications
{
    public class NotificationDeviceRepository : INotificationDeviceRepository
    {
        private readonly AppDbContext _context;

        public NotificationDeviceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<NotificationDevice>> GetActiveForUserAsync(Guid userId)
        {
            return await _context.Set<NotificationDevice>()
                .AsNoTracking()
                .Where(d => d.UserId == userId && d.IsActive)
                .ToListAsync();
        }

        public async Task<NotificationDevice?> GetByPlayerIdAsync(PushProvider provider, string playerId)
        {
            return await _context.Set<NotificationDevice>()
                .FirstOrDefaultAsync(d => d.Provider == provider && d.PlayerId == playerId);
        }

        public async Task<bool> DeactivateAsync(PushProvider provider, string playerId)
        {
            var changed = await _context.Set<NotificationDevice>()
                .Where(d => d.Provider == provider && d.PlayerId == playerId && d.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsActive, false));
            return changed > 0;
        }

        public async Task AddAsync(NotificationDevice device)
        {
            ArgumentNullException.ThrowIfNull(device);
            await _context.Set<NotificationDevice>().AddAsync(device);
        }

        public void Update(NotificationDevice device)
        {
            ArgumentNullException.ThrowIfNull(device);
            _context.Set<NotificationDevice>().Update(device);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
