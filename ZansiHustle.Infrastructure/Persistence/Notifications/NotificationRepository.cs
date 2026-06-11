using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Notifications;
using ZansiHustle.Domain.Notifications;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Notifications
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _context;

        public NotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Notification>> GetForUserAsync(Guid userId, int take)
        {
            return await _context.Set<Notification>()
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<Notification?> GetByIdAsync(Guid id)
        {
            return await _context.Set<Notification>().FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<int> MarkAllReadAsync(Guid userId, DateTime nowUtc)
        {
            // EF Core 8 bulk update — no entities loaded into the change tracker.
            return await _context.Set<Notification>()
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.IsRead, true)
                    .SetProperty(n => n.ReadAtUtc, nowUtc));
        }

        public async Task AddAsync(Notification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            await _context.Set<Notification>().AddAsync(notification);
        }

        public void Update(Notification notification)
        {
            ArgumentNullException.ThrowIfNull(notification);
            _context.Set<Notification>().Update(notification);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
