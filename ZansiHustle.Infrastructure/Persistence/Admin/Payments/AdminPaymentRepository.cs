using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Admin.Payments.Dtos;
using ZansiHustle.Application.Persistence.Admin.Payments;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Orders;

namespace ZansiHustle.Infrastructure.Persistence.Admin.Payments
{
    /// <summary>
    /// EF Core-backed queries for the admin Payments page.
    ///
    /// Money flow is derived from Order.PaymentStatus — the platform does not
    /// yet operate its own Payment ledger independently of orders. KPIs bucket
    /// orders by payment status and return both the gross total and a count
    /// per bucket; the payout queue is intentionally empty today because we
    /// have no Payout entity to project.
    /// </summary>
    public class AdminPaymentRepository : IAdminPaymentRepository
    {
        private readonly AppDbContext _context;

        public AdminPaymentRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PaymentsKpisDto> GetKpisAsync()
        {
            // Single grouped query keeps the round-trip count to one — each
            // bucket emits its sum and its count.
            var buckets = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.PaymentStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Total = g.Sum(o => (decimal?)o.Total) ?? 0m,
                    Count = g.Count(),
                })
                .ToListAsync();

            decimal TotalFor(PaymentStatus s) => buckets.FirstOrDefault(b => b.Status == s)?.Total ?? 0m;
            int CountFor(PaymentStatus s) => buckets.FirstOrDefault(b => b.Status == s)?.Count ?? 0;

            var totalGmv = TotalFor(PaymentStatus.Paid);
            var pendingPayments = TotalFor(PaymentStatus.Pending);
            var failedPayments = TotalFor(PaymentStatus.Failed);
            var refunded = TotalFor(PaymentStatus.Refunded);

            return new PaymentsKpisDto
            {
                TotalGMV = totalGmv,
                NetRevenue = totalGmv,     // no commission model yet
                PendingPayouts = 0m,        // no Payout entity yet
                TotalRevenue = totalGmv,
                PendingPayments = pendingPayments,
                FailedPayments = failedPayments,
                Refunded = refunded,
                PaidCount = CountFor(PaymentStatus.Paid),
                PendingCount = CountFor(PaymentStatus.Pending),
                FailedCount = CountFor(PaymentStatus.Failed),
                RefundedCount = CountFor(PaymentStatus.Refunded),
            };
        }

        public Task<List<AdminPayoutListItemDto>> GetPayoutQueueAsync(int limit)
        {
            // No Payout entity exists — return an explicit empty list so the
            // UI gets a clean state rather than "API not implemented". Swap
            // this implementation in once Payouts are modeled.
            return Task.FromResult(new List<AdminPayoutListItemDto>());
        }
    }
}
