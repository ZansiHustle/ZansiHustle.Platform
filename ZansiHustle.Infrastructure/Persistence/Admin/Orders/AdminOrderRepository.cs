using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Admin.Orders.Dtos;
using ZansiHustle.Application.Persistence.Admin.Orders;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Persistence.Admin.Orders
{
    /// <summary>
    /// EF Core-backed queries for the admin Orders page.
    ///
    /// KPI totals are counted over ALL orders (any payment status) — the page
    /// shows raw order throughput. <see cref="GetPagedAsync"/> returns any
    /// status so the filter tabs on the page can toggle between them.
    /// The <c>AvgOrderValue</c> KPI is restricted to paid orders so it
    /// reflects realized revenue.
    /// </summary>
    public class AdminOrderRepository : IAdminOrderRepository
    {
        private readonly AppDbContext _context;

        public AdminOrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrdersKpisDto> GetKpisAsync()
        {
            var now = DateTime.UtcNow;
            var startOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var startOfWeek = startOfToday.AddDays(-6);
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var orders = _context.Orders.AsNoTracking();

            var ordersToday = await orders.CountAsync(o => o.CreatedAtUtc >= startOfToday);
            var ordersThisWeek = await orders.CountAsync(o => o.CreatedAtUtc >= startOfWeek);
            var ordersThisMonth = await orders.CountAsync(o => o.CreatedAtUtc >= startOfMonth);

            var paidOrders = orders.Where(o => o.PaymentStatus == PaymentStatus.Paid);
            var paidTotal = await paidOrders.SumAsync(o => (decimal?)o.Total) ?? 0m;
            var paidCount = await paidOrders.CountAsync();
            var avgOrderValue = paidCount > 0 ? Math.Round(paidTotal / paidCount, 2) : 0m;

            return new OrdersKpisDto
            {
                OrdersToday = ordersToday,
                OrdersThisWeek = ordersThisWeek,
                OrdersThisMonth = ordersThisMonth,
                AvgOrderValue = avgOrderValue,
            };
        }

        public async Task<PagedResult<AdminOrderListItemDto>> GetPagedAsync(PagedListQueryBase query)
        {
            var q = _context.Orders.AsNoTracking();

            // Status filter — accept the lowercase enum-style strings the
            // portal sends (e.g. "pending", "in_progress").
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var mapped = MapStatusFilter(query.Status);
                if (mapped.HasValue)
                {
                    q = q.Where(o => o.Status == mapped.Value);
                }
            }

            // Date range on order creation.
            if (query.FromUtc.HasValue)
            {
                var from = DateTime.SpecifyKind(query.FromUtc.Value, DateTimeKind.Utc);
                q = q.Where(o => o.CreatedAtUtc >= from);
            }
            if (query.ToUtc.HasValue)
            {
                var to = DateTime.SpecifyKind(query.ToUtc.Value, DateTimeKind.Utc);
                q = q.Where(o => o.CreatedAtUtc <= to);
            }

            // Search across code, buyer name, buyer email, and merchant name.
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                q = q.Where(o =>
                    (o.Code != null && EF.Functions.Like(o.Code, $"%{term}%")) ||
                    (o.BuyerName != null && EF.Functions.Like(o.BuyerName, $"%{term}%")) ||
                    (o.BuyerEmail != null && EF.Functions.Like(o.BuyerEmail, $"%{term}%")) ||
                    (o.Merchant != null && o.Merchant.Name != null && EF.Functions.Like(o.Merchant.Name, $"%{term}%"))
                );
            }

            var totalCount = await q.CountAsync();

            var skip = (query.Page - 1) * query.PageSize;
            var rows = await q
                .OrderByDescending(o => o.CreatedAtUtc)
                .Skip(skip)
                .Take(query.PageSize)
                .Select(o => new
                {
                    o.Id,
                    o.Code,
                    o.BuyerName,
                    o.BuyerEmail,
                    o.Total,
                    o.Currency,
                    o.Status,
                    o.PaymentStatus,
                    o.CreatedAtUtc,
                    ShopName = o.Merchant != null ? o.Merchant.Name : string.Empty,
                })
                .ToListAsync();

            var items = rows.Select(r => new AdminOrderListItemDto
            {
                Id = string.IsNullOrWhiteSpace(r.Code) ? r.Id.ToString() : r.Code,
                Customer = !string.IsNullOrWhiteSpace(r.BuyerName)
                    ? r.BuyerName!
                    : (r.BuyerEmail ?? "Customer"),
                Shop = r.ShopName ?? string.Empty,
                Amount = r.Total,
                Currency = r.Currency,
                Status = MapStatus(r.Status),
                PaymentStatus = MapPaymentStatus(r.PaymentStatus),
                Date = r.CreatedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            }).ToList();

            return new PagedResult<AdminOrderListItemDto>
            {
                Items = items,
                Total = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
            };
        }

        /// <summary>Parses the portal's lowercase status filter into the OrderStatus enum.</summary>
        private static OrderStatus? MapStatusFilter(string status) => status.ToLowerInvariant() switch
        {
            "pending" => OrderStatus.Pending,
            "confirmed" => OrderStatus.Confirmed,
            "in_progress" => OrderStatus.InProgress,
            "completed" => OrderStatus.Completed,
            "cancelled" => OrderStatus.Cancelled,
            _ => (OrderStatus?)null,
        };

        private static string MapStatus(OrderStatus status) => status switch
        {
            OrderStatus.Pending => "pending",
            OrderStatus.Confirmed => "confirmed",
            OrderStatus.InProgress => "in_progress",
            OrderStatus.Completed => "completed",
            OrderStatus.Cancelled => "cancelled",
            _ => status.ToString().ToLowerInvariant(),
        };

        private static string MapPaymentStatus(PaymentStatus status) => status switch
        {
            PaymentStatus.Pending => "pending",
            PaymentStatus.Paid => "paid",
            PaymentStatus.Failed => "failed",
            PaymentStatus.Refunded => "refunded",
            _ => status.ToString().ToLowerInvariant(),
        };
    }
}
