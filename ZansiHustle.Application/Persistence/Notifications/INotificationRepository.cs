using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Notifications;

namespace ZansiHustle.Application.Persistence.Notifications
{
    public interface INotificationRepository
    {
        /// <summary>Newest-first notifications for a user, capped at <paramref name="take"/>.</summary>
        Task<List<Notification>> GetForUserAsync(Guid userId, int take);

        /// <summary>Count of unread notifications for a user.</summary>
        Task<int> GetUnreadCountAsync(Guid userId);

        /// <summary>A single notification by id (tracked) — null when not found.</summary>
        Task<Notification?> GetByIdAsync(Guid id);

        /// <summary>Mark every unread notification for a user as read. Returns rows changed.</summary>
        Task<int> MarkAllReadAsync(Guid userId, DateTime nowUtc);

        Task AddAsync(Notification notification);
        void Update(Notification notification);
        Task<bool> SaveChangesAsync();
    }
}
