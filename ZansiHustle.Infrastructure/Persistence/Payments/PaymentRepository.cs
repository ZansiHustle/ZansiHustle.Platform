using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Payments;
using ZansiHustle.Domain.Payments;
using ZansiHustle.Shared.Enums.Payments;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.Payments
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly AppDbContext _context;

        public PaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _context.Payments
                .Include(x => x.Order)
                .Include(x => x.Events)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<Payment?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            var normalised = code.Trim();

            return await _context.Payments
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.Code == normalised);
        }

        public async Task<Payment?> GetByProviderReferenceAsync(string providerReference)
        {
            if (string.IsNullOrWhiteSpace(providerReference)) return null;

            var normalised = providerReference.Trim();

            return await _context.Payments
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.ProviderReference == normalised);
        }

        public async Task<List<Payment>> GetByOrderAsync(Guid orderId)
        {
            return await _context.Payments
                .AsNoTracking()
                .Where(x => x.OrderId == orderId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Payment?> GetActiveAttemptForOrderAsync(Guid orderId)
        {
            // An "active attempt" is the most recent non-terminal payment row.
            // If found, `InitializeAsync` reuses it so the caller always gets
            // the same authorization URL on retries.
            return await _context.Payments
                .Include(x => x.Order)
                .Where(x => x.OrderId == orderId)
                .Where(x => x.Status == PaymentTransactionStatus.Initialized
                         || x.Status == PaymentTransactionStatus.Pending)
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> ExistsByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;

            var normalised = code.Trim();

            return await _context.Payments.AnyAsync(x => x.Code == normalised);
        }

        public async Task AddAsync(Payment payment)
        {
            ArgumentNullException.ThrowIfNull(payment);

            await _context.Payments.AddAsync(payment);
        }

        public void Update(Payment payment)
        {
            ArgumentNullException.ThrowIfNull(payment);

            _context.Payments.Update(payment);
        }

        public async Task AddEventAsync(PaymentEvent paymentEvent)
        {
            ArgumentNullException.ThrowIfNull(paymentEvent);

            await _context.PaymentEvents.AddAsync(paymentEvent);
        }

        public async Task<bool> EventKeyExistsAsync(string providerEventKey)
        {
            if (string.IsNullOrWhiteSpace(providerEventKey)) return false;

            return await _context.PaymentEvents.AnyAsync(x => x.ProviderEventKey == providerEventKey);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
