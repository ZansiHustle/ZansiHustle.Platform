using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Notifications;
using ZansiHustle.Shared.Enums.Notifications;

namespace ZansiHustle.Application.Persistence.Notifications
{
    public interface INotificationDeviceRepository
    {
        /// <summary>Active push devices for a user (delivery targets).</summary>
        Task<List<NotificationDevice>> GetActiveForUserAsync(Guid userId);

        /// <summary>Existing device for (provider, playerId) — for upsert. Tracked.</summary>
        Task<NotificationDevice?> GetByPlayerIdAsync(PushProvider provider, string playerId);

        /// <summary>Deactivate a player id (logout). Returns true if a row changed.</summary>
        Task<bool> DeactivateAsync(PushProvider provider, string playerId);

        Task AddAsync(NotificationDevice device);
        void Update(NotificationDevice device);
        Task<bool> SaveChangesAsync();
    }
}
