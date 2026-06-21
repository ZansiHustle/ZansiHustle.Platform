using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Shared.Enums.ServiceBookings;

namespace ZansiHustle.Application.Persistence.ServiceBookings
{
    /// <summary>
    /// Persistence contract for <see cref="ServiceBookingReview"/>. List /
    /// directional queries scope to <c>ReviewStatus.Active</c>;
    /// <see cref="GetByIdAsync"/> returns the row regardless so the service can
    /// act on soft-deleted rows later.
    /// </summary>
    public interface IServiceBookingReviewRepository
    {
        /// <summary>Loads a single review by id, regardless of status.</summary>
        Task<ServiceBookingReview?> GetByIdAsync(Guid id);

        /// <summary>All Active reviews for a booking (both directions).</summary>
        Task<List<ServiceBookingReview>> GetActiveByBookingAsync(Guid bookingId);

        /// <summary>The single Active review for a booking + direction, if any.</summary>
        Task<ServiceBookingReview?> GetActiveByDirectionAsync(
            Guid bookingId, ServiceBookingReviewDirection direction);

        Task AddAsync(ServiceBookingReview review);
        void Update(ServiceBookingReview review);
        Task<bool> SaveChangesAsync();
    }
}
