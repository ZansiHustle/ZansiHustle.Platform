using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.ServiceBookings;

namespace ZansiHustle.Application.Persistence.ServiceBookings
{
    public interface IServiceBookingRepository
    {
        /// <summary>
        /// Active bookings for a provider that overlap [fromUtc, toUtc). "Active"
        /// = blocks a slot: Confirmed / Accepted / Completed always, plus
        /// PendingPayment created within the hold window (stale pending are
        /// excluded so abandoned checkouts release their slot). Cancelled /
        /// Rejected are never returned.
        /// </summary>
        Task<List<ServiceBooking>> GetActiveForMerchantInRangeAsync(
            Guid merchantId, DateTime fromUtc, DateTime toUtc, DateTime nowUtc);

        /// <summary>
        /// True when an active booking for the provider overlaps the given UTC
        /// window — the order-creation race guard.
        /// </summary>
        Task<bool> HasActiveOverlapAsync(
            Guid merchantId, DateTime startAtUtc, DateTime endAtUtc, DateTime nowUtc);

        /// <summary>All bookings attached to an order (for payment-status sync).</summary>
        Task<List<ServiceBooking>> GetByOrderAsync(Guid orderId);

        Task AddAsync(ServiceBooking booking);
        void Update(ServiceBooking booking);
        Task<bool> SaveChangesAsync();
    }
}
