using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Reviews;
using ZansiHustle.Shared.Enums.ServiceBookings;

namespace ZansiHustle.Infrastructure.Persistence.ServiceBookings
{
    public class ServiceBookingReviewRepository : IServiceBookingReviewRepository
    {
        private readonly AppDbContext _context;

        public ServiceBookingReviewRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public Task<ServiceBookingReview?> GetByIdAsync(Guid id)
            => _context.ServiceBookingReviews.FirstOrDefaultAsync(r => r.Id == id);

        /// <inheritdoc />
        public async Task<List<ServiceBookingReview>> GetActiveByBookingAsync(Guid bookingId)
        {
            return await _context.ServiceBookingReviews
                .AsNoTracking()
                .Where(r => r.BookingId == bookingId && r.Status == ReviewStatus.Active)
                .OrderByDescending(r => r.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public Task<ServiceBookingReview?> GetActiveByDirectionAsync(
            Guid bookingId, ServiceBookingReviewDirection direction)
        {
            return _context.ServiceBookingReviews.FirstOrDefaultAsync(r =>
                r.BookingId == bookingId
                && r.Direction == direction
                && r.Status == ReviewStatus.Active);
        }

        /// <inheritdoc />
        public async Task AddAsync(ServiceBookingReview review)
            => await _context.ServiceBookingReviews.AddAsync(review);

        /// <inheritdoc />
        public void Update(ServiceBookingReview review)
            => _context.ServiceBookingReviews.Update(review);

        /// <inheritdoc />
        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() > 0;
    }
}
