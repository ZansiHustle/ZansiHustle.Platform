using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Admin.Customers.Dtos;
using ZansiHustle.Application.Persistence.Admin.Customers;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Shared.Queries;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Persistence.Admin.Customers
{
    /// <summary>
    /// EF Core-backed queries for the admin Customers page. Customers are
    /// derived from Orders by grouping on <c>BuyerUserId</c>. Order counts
    /// use all orders (any status); lifetime spend only counts
    /// <see cref="PaymentStatus.Paid"/> orders.
    /// </summary>
    public class AdminCustomerRepository : IAdminCustomerRepository
    {
        private readonly AppDbContext _context;

        public AdminCustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CustomersKpisDto> GetKpisAsync()
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var perBuyer = await _context.Orders.AsNoTracking()
                .GroupBy(o => o.BuyerUserId)
                .Select(g => new
                {
                    OrderCount = g.Count(),
                    ActiveThisMonth = g.Any(o => o.CreatedAtUtc >= startOfMonth),
                })
                .ToListAsync();

            var totalCustomers = perBuyer.Count;
            var totalOrders = perBuyer.Sum(b => b.OrderCount);
            var activeThisMonth = perBuyer.Count(b => b.ActiveThisMonth);
            var returningCount = perBuyer.Count(b => b.OrderCount > 1);

            var avgOrdersPerCustomer = totalCustomers > 0
                ? Math.Round((decimal)totalOrders / totalCustomers, 1)
                : 0m;
            var returningRate = totalCustomers > 0
                ? Math.Round((decimal)returningCount / totalCustomers * 100m, 1)
                : 0m;

            return new CustomersKpisDto
            {
                TotalCustomers = totalCustomers,
                ActiveThisMonth = activeThisMonth,
                AvgOrdersPerCustomer = avgOrdersPerCustomer,
                ReturningCustomerRate = returningRate,
            };
        }

        public async Task<PagedResult<AdminCustomerListItemDto>> GetPagedAsync(PagedListQueryBase query)
        {
            var activeCutoff = DateTime.UtcNow.AddDays(-AdminCustomerListItemDto.ActiveWithinDays);

            // Base grouped query — one row per buyer with all aggregates the
            // DTO needs. Filtering is applied on the aggregate projection so
            // status filters (active/inactive) see the real last-order date.
            var aggregateQuery = _context.Orders.AsNoTracking()
                .GroupBy(o => o.BuyerUserId)
                .Select(g => new
                {
                    BuyerUserId = g.Key,
                    OrderCount = g.Count(),
                    Spent = g.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => (decimal?)o.Total) ?? 0m,
                    FirstOrder = g.Min(o => o.CreatedAtUtc),
                    LastOrder = g.Max(o => o.CreatedAtUtc),
                    LatestName = g.OrderByDescending(o => o.CreatedAtUtc).Select(o => o.BuyerName).FirstOrDefault(),
                    LatestEmail = g.OrderByDescending(o => o.CreatedAtUtc).Select(o => o.BuyerEmail).FirstOrDefault(),
                    Currency = g.OrderByDescending(o => o.CreatedAtUtc).Select(o => o.Currency).FirstOrDefault(),
                });

            // Status filter: "active" = last order within window, "inactive" = older.
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var s = query.Status.ToLowerInvariant();
                if (s == "active")
                    aggregateQuery = aggregateQuery.Where(r => r.LastOrder >= activeCutoff);
                else if (s == "inactive")
                    aggregateQuery = aggregateQuery.Where(r => r.LastOrder < activeCutoff);
            }

            // Date range applies to the FirstOrder ("joined") date — matches
            // how the UI labels the column.
            if (query.FromUtc.HasValue)
            {
                var from = DateTime.SpecifyKind(query.FromUtc.Value, DateTimeKind.Utc);
                aggregateQuery = aggregateQuery.Where(r => r.FirstOrder >= from);
            }
            if (query.ToUtc.HasValue)
            {
                var to = DateTime.SpecifyKind(query.ToUtc.Value, DateTimeKind.Utc);
                aggregateQuery = aggregateQuery.Where(r => r.FirstOrder <= to);
            }

            // Search across the buyer's most recent name + email.
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                aggregateQuery = aggregateQuery.Where(r =>
                    (r.LatestName != null && EF.Functions.Like(r.LatestName, $"%{term}%")) ||
                    (r.LatestEmail != null && EF.Functions.Like(r.LatestEmail, $"%{term}%"))
                );
            }

            var totalCount = await aggregateQuery.CountAsync();

            var skip = (query.Page - 1) * query.PageSize;
            var rows = await aggregateQuery
                .OrderByDescending(r => r.LastOrder)
                .Skip(skip)
                .Take(query.PageSize)
                .ToListAsync();

            var items = rows.Select(r => new AdminCustomerListItemDto
            {
                Id = r.BuyerUserId.ToString(),
                Name = !string.IsNullOrWhiteSpace(r.LatestName)
                    ? r.LatestName!
                    : (r.LatestEmail ?? "Customer"),
                Email = r.LatestEmail ?? string.Empty,
                City = string.Empty,
                Orders = r.OrderCount,
                Spent = r.Spent,
                Currency = r.Currency ?? "ZAR",
                ReferredBy = null,
                Status = r.LastOrder >= activeCutoff ? "active" : "inactive",
                Joined = r.FirstOrder.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                LastOrder = r.LastOrder.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            }).ToList();

            return new PagedResult<AdminCustomerListItemDto>
            {
                Items = items,
                Total = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
            };
        }
    }
}
