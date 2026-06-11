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

        /// <summary>
        /// Bookings attached to an order WITH Merchant + Listing included — used
        /// by the payment-paid path so the seller notification can read the
        /// listing title and merchant owner without extra round-trips. Tracked.
        /// </summary>
        Task<List<ServiceBooking>> GetByOrderWithDetailsAsync(Guid orderId);

        /// <summary>Single booking with Order + Merchant + Listing, tracked
        /// (for action endpoints that mutate it). Null when not found.</summary>
        Task<ServiceBooking?> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// Bookings for shops owned by <paramref name="sellerUserId"/> that are
        /// genuine post-payment requests/work — Requested/Accepted/InProgress/
        /// Completed (legacy Confirmed included). NEVER PendingPayment, Cancelled
        /// or Rejected. Newest first. Backed by a server-side filter so the
        /// client can't be relied on to hide failed-payment bookings.
        /// </summary>
        Task<List<ServiceBooking>> GetForSellerAsync(Guid sellerUserId);

        Task AddAsync(ServiceBooking booking);
        void Update(ServiceBooking booking);
        Task<bool> SaveChangesAsync();
    }
}
