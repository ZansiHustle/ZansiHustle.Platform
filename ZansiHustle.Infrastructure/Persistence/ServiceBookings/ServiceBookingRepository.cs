using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.ServiceBookings;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.ServiceBookings;

namespace ZansiHustle.Infrastructure.Persistence.ServiceBookings
{
    public class ServiceBookingRepository : IServiceBookingRepository
    {
        private readonly AppDbContext _context;

        public ServiceBookingRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// A booking counts as an active hold (blocks a slot) when it is
        /// Confirmed/Accepted/Completed, OR PendingPayment created within the
        /// hold window. Stale PendingPayment (older than the hold) and
        /// Cancelled/Rejected/Requested are excluded.
        /// </summary>
        private static IQueryable<ServiceBooking> ActiveFilter(
            IQueryable<ServiceBooking> q, DateTime nowUtc)
        {
            var staleBefore = nowUtc.AddMinutes(-BookingAvailabilityDefaults.PendingPaymentHoldMinutes);
            return q.Where(b =>
                b.Status == ServiceBookingStatus.Requested      // paid, awaiting acceptance
                || b.Status == ServiceBookingStatus.Confirmed   // legacy paid state
                || b.Status == ServiceBookingStatus.Accepted
                || b.Status == ServiceBookingStatus.InProgress
                || b.Status == ServiceBookingStatus.Completed
                || (b.Status == ServiceBookingStatus.PendingPayment && b.CreatedAtUtc >= staleBefore));
        }

        /// <inheritdoc />
        public async Task<List<ServiceBooking>> GetActiveForMerchantInRangeAsync(
            Guid merchantId, DateTime fromUtc, DateTime toUtc, DateTime nowUtc)
        {
            var query = _context.Set<ServiceBooking>().AsNoTracking()
                .Where(b => b.MerchantId == merchantId
                            && b.StartAtUtc < toUtc
                            && b.EndAtUtc > fromUtc);
            return await ActiveFilter(query, nowUtc)
                .OrderBy(b => b.StartAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<bool> HasActiveOverlapAsync(
            Guid merchantId, DateTime startAtUtc, DateTime endAtUtc, DateTime nowUtc)
        {
            // Overlap: existing.Start < new.End && existing.End > new.Start.
            var query = _context.Set<ServiceBooking>().AsNoTracking()
                .Where(b => b.MerchantId == merchantId
                            && b.StartAtUtc < endAtUtc
                            && b.EndAtUtc > startAtUtc);
            return await ActiveFilter(query, nowUtc).AnyAsync();
        }

        /// <inheritdoc />
        public async Task<List<ServiceBooking>> GetByOrderAsync(Guid orderId)
        {
            return await _context.Set<ServiceBooking>()
                .Where(b => b.OrderId == orderId)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Dictionary<Guid, ServiceBookingStatus>> GetStatusesByOrderIdsAsync(
            IReadOnlyCollection<Guid> orderIds)
        {
            if (orderIds is null || orderIds.Count == 0)
                return new Dictionary<Guid, ServiceBookingStatus>();

            // One query; oldest-first so the newest booking per order wins the
            // dictionary slot (orders are 1:1 with a booking in practice).
            var rows = await _context.Set<ServiceBooking>()
                .AsNoTracking()
                .Where(b => orderIds.Contains(b.OrderId))
                .OrderBy(b => b.CreatedAtUtc)
                .Select(b => new { b.OrderId, b.Status })
                .ToListAsync();

            var map = new Dictionary<Guid, ServiceBookingStatus>();
            foreach (var r in rows) map[r.OrderId] = r.Status;
            return map;
        }

        /// <inheritdoc />
        public async Task<List<ServiceBooking>> GetByOrderWithDetailsAsync(Guid orderId)
        {
            return await _context.Set<ServiceBooking>()
                .Include(b => b.Order)
                .Include(b => b.Merchant)
                .Include(b => b.Listing)
                .Where(b => b.OrderId == orderId)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<ServiceBooking?> GetByIdWithDetailsAsync(Guid id)
        {
            return await _context.Set<ServiceBooking>()
                .Include(b => b.Order)
                .Include(b => b.Merchant)
                .Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        /// <inheritdoc />
        public async Task<List<ServiceBooking>> GetForSellerAsync(Guid sellerUserId)
        {
            var visible = new[]
            {
                ServiceBookingStatus.Requested,
                ServiceBookingStatus.Confirmed,
                ServiceBookingStatus.Accepted,
                ServiceBookingStatus.InProgress,
                ServiceBookingStatus.Completed
            };

            return await _context.Set<ServiceBooking>()
                .AsNoTracking()
                .Include(b => b.Order)
                .Include(b => b.Listing)
                .Where(b => b.Merchant != null
                            && b.Merchant.OwnerUserId == sellerUserId
                            && visible.Contains(b.Status))
                .OrderByDescending(b => b.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task AddAsync(ServiceBooking booking)
        {
            ArgumentNullException.ThrowIfNull(booking);
            await _context.Set<ServiceBooking>().AddAsync(booking);
        }

        /// <inheritdoc />
        public void Update(ServiceBooking booking)
        {
            ArgumentNullException.ThrowIfNull(booking);
            _context.Set<ServiceBooking>().Update(booking);
        }

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
