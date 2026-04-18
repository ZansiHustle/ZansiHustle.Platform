using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Analytics.Dtos;
using ZansiHustle.Application.Persistence.Analytics;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Orders;

namespace ZansiHustle.Infrastructure.Persistence.Analytics
{
    /// <summary>
    /// EF Core-backed analytics aggregation queries.
    ///
    /// Every "revenue" figure is derived from orders in
    /// <see cref="PaymentStatus.Paid"/> — pending/failed/refunded orders are
    /// deliberately excluded so the dashboard mirrors the GMV a finance team
    /// would actually recognize. Fields that the schema does not yet track
    /// (affiliates, support tickets, disputes, platform profit) are returned
    /// as zero so the UI never sees null and the gap is obvious to anyone
    /// reading the DTO.
    /// </summary>
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private readonly AppDbContext _context;

        public AnalyticsRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AnalyticsKpisDto> GetKpisAsync()
        {
            var now = DateTime.UtcNow;
            var startOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var startOfWeek = startOfToday.AddDays(-6); // last 7 days inclusive
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var paidOrders = _context.Orders.AsNoTracking().Where(o => o.PaymentStatus == PaymentStatus.Paid);

            var totalGmv = await paidOrders.SumAsync(o => (decimal?)o.Total) ?? 0m;
            var paidOrderCount = await paidOrders.CountAsync();

            var avgOrderValue = paidOrderCount > 0
                ? Math.Round(totalGmv / paidOrderCount, 2)
                : 0m;

            var ordersToday = await _context.Orders.AsNoTracking()
                .CountAsync(o => o.CreatedAtUtc >= startOfToday);
            var ordersThisWeek = await _context.Orders.AsNoTracking()
                .CountAsync(o => o.CreatedAtUtc >= startOfWeek);
            var ordersThisMonth = await _context.Orders.AsNoTracking()
                .CountAsync(o => o.CreatedAtUtc >= startOfMonth);

            var totalCustomers = await _context.Orders.AsNoTracking()
                .Select(o => o.BuyerUserId)
                .Distinct()
                .CountAsync();

            // Returning customer rate: % of buyers who have placed more than
            // one order (any status). Null guard for empty datasets.
            var buyerOrderCounts = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.BuyerUserId)
                .Select(g => g.Count())
                .ToListAsync();
            var returningCustomerRate = buyerOrderCounts.Count > 0
                ? Math.Round((decimal)buyerOrderCounts.Count(c => c > 1) / buyerOrderCounts.Count * 100m, 1)
                : 0m;

            var activeMerchants = await _context.Merchants.AsNoTracking()
                .CountAsync(m => m.Status == MerchantStatus.Active);

            return new AnalyticsKpisDto
            {
                TotalGMV = totalGmv,
                NetRevenue = totalGmv,           // no commission model yet
                PlatformProfit = 0m,             // no cost model yet
                PendingPayouts = 0m,             // no payout entity yet
                ActiveSellers = activeMerchants,
                ActiveShops = activeMerchants,   // 1:1 in v1
                ActiveAffiliates = 0,            // affiliate module not live
                OpenTickets = 0,
                UnresolvedDisputes = 0,
                OrdersToday = ordersToday,
                OrdersThisWeek = ordersThisWeek,
                OrdersThisMonth = ordersThisMonth,
                TotalCustomers = totalCustomers,
                ConversionRate = 0m,             // no traffic analytics yet
                AvgOrderValue = avgOrderValue,
                ReturningCustomerRate = returningCustomerRate,
            };
        }

        public async Task<List<RevenuePointDto>> GetRevenueTrendAsync(int months)
        {
            var now = DateTime.UtcNow;
            // Inclusive window: start on the first day of (now - (months-1)) and end at `now`.
            var windowStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(months - 1));

            // Group paid orders by (year, month). EF translates this to a SQL
            // GROUP BY — far cheaper than pulling rows and grouping in memory.
            var grouped = await _context.Orders.AsNoTracking()
                .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.CreatedAtUtc >= windowStart)
                .GroupBy(o => new { o.CreatedAtUtc.Year, o.CreatedAtUtc.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Revenue = g.Sum(o => o.Total),
                    Orders = g.Count(),
                })
                .ToListAsync();

            var byKey = grouped.ToDictionary(g => (g.Year, g.Month), g => g);

            // Emit a datapoint for every month in the window, even zero ones,
            // so the chart axis is continuous.
            var points = new List<RevenuePointDto>(months);
            for (var i = 0; i < months; i++)
            {
                var marker = windowStart.AddMonths(i);
                var label = marker.ToString("MMM", CultureInfo.InvariantCulture);
                if (byKey.TryGetValue((marker.Year, marker.Month), out var agg))
                {
                    points.Add(new RevenuePointDto
                    {
                        Month = label,
                        Revenue = agg.Revenue,
                        Orders = agg.Orders,
                        // No cost model — profit mirrors revenue until we track fees.
                        Profit = agg.Revenue,
                    });
                }
                else
                {
                    points.Add(new RevenuePointDto { Month = label, Revenue = 0m, Orders = 0, Profit = 0m });
                }
            }

            return points;
        }

        public async Task<List<RegionBreakdownDto>> GetRegionBreakdownAsync()
        {
            // Merchants per province.
            var sellerCounts = await _context.Merchants.AsNoTracking()
                .Where(m => !string.IsNullOrWhiteSpace(m.Province))
                .GroupBy(m => m.Province!)
                .Select(g => new { Province = g.Key, Count = g.Count() })
                .ToListAsync();

            // Revenue per province (join paid orders → merchant → province).
            var revenueByProvince = await _context.Orders.AsNoTracking()
                .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Merchant != null && !string.IsNullOrWhiteSpace(o.Merchant.Province))
                .GroupBy(o => o.Merchant!.Province!)
                .Select(g => new { Province = g.Key, Revenue = g.Sum(o => o.Total) })
                .ToListAsync();

            var revenueLookup = revenueByProvince.ToDictionary(x => x.Province, x => x.Revenue);

            return sellerCounts
                .Select(sc => new RegionBreakdownDto
                {
                    Name = sc.Province,
                    Sellers = sc.Count,
                    Shops = sc.Count, // 1:1 in v1
                    Revenue = revenueLookup.TryGetValue(sc.Province, out var rev) ? rev : 0m,
                })
                .OrderByDescending(r => r.Revenue)
                .ThenByDescending(r => r.Sellers)
                .ToList();
        }

        public async Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync()
        {
            // Paid revenue per seller category. "Uncategorized" captures
            // merchants without a category assignment so totals still reconcile
            // to the platform GMV.
            var rows = await _context.Orders.AsNoTracking()
                .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.Merchant != null)
                .Select(o => new
                {
                    Category = o.Merchant!.SellerCategory != null ? o.Merchant.SellerCategory.Name : "Uncategorized",
                    o.Total,
                })
                .ToListAsync();

            var grouped = rows
                .GroupBy(r => r.Category)
                .Select(g => new { Name = g.Key, Revenue = g.Sum(r => r.Total) })
                .ToList();

            var total = grouped.Sum(g => g.Revenue);

            return grouped
                .Select(g => new CategoryBreakdownDto
                {
                    Name = g.Name,
                    Revenue = g.Revenue,
                    Value = total > 0 ? Math.Round(g.Revenue / total * 100m, 0) : 0m,
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();
        }
    }
}
