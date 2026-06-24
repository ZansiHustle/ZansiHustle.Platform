using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Analytics.Dtos;
using ZansiHustle.Application.Persistence.Analytics;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Marketplace;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.Payments;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Enums.ZansiDispatch;

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

        public async Task<AnalyticsOverviewDto> GetOverviewAsync(int growthMonths)
        {
            var now = DateTime.UtcNow;

            // ── Service-order ids: an Order is a SERVICE order when a ServiceBooking
            // points at it; everything else is a product order. Computed once and
            // reused so the product/service split is consistent. ──────────────────
            var serviceOrderIds = _context.ServiceBookings.AsNoTracking().Select(b => b.OrderId);

            var orders = _context.Orders.AsNoTracking();
            var productOrders = orders.Where(o => !serviceOrderIds.Contains(o.Id));
            var paidProductOrders = productOrders.Where(o => o.PaymentStatus == PaymentStatus.Paid);

            // ── Summary ─────────────────────────────────────────────────────────
            // Customers = every registered user account (the platform's user base —
            // all sign up as customers first). Sellers = active merchant records.
            // Shops = real ShopProfile rows. Active listings = seller Listings in
            // Active status. Gross sales = SUM(Total) over PAID orders only.
            var totalCustomers = await _context.Users.AsNoTracking().CountAsync();
            var totalSellers = await _context.Merchants.AsNoTracking().CountAsync(m => m.Status == MerchantStatus.Active);
            var totalShops = await _context.ShopProfiles.AsNoTracking().CountAsync();
            var activeListings = await _context.Listings.AsNoTracking().CountAsync(l => l.Status == ListingStatus.Active);
            var productOrderCount = await productOrders.CountAsync();
            var serviceBookingCount = await _context.ServiceBookings.AsNoTracking().CountAsync();
            var grossSales = await orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).SumAsync(o => (decimal?)o.Total) ?? 0m;
            var successfulPayments = await _context.Payments.AsNoTracking().CountAsync(p => p.Status == PaymentTransactionStatus.Succeeded);
            var cancelledFailedPayments = await _context.Payments.AsNoTracking()
                .CountAsync(p => p.Status == PaymentTransactionStatus.Failed || p.Status == PaymentTransactionStatus.Cancelled);
            var ordersAwaitingAcceptance = await productOrders.CountAsync(o => o.Status == OrderStatus.AwaitingSellerAcceptance);
            var requestedBookings = await _context.ServiceBookings.AsNoTracking().CountAsync(b => b.Status == ServiceBookingStatus.Requested);

            // ── User growth (NEW registrations per month, real counts) ───────────
            var windowStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(growthMonths - 1));

            var customerByMonth = await _context.Users.AsNoTracking()
                .Where(u => u.CreatedOnUtc >= windowStart)
                .GroupBy(u => new { u.CreatedOnUtc.Year, u.CreatedOnUtc.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();
            var sellerByMonth = await _context.Merchants.AsNoTracking()
                .Where(m => m.CreatedAtUtc >= windowStart)
                .GroupBy(m => new { m.CreatedAtUtc.Year, m.CreatedAtUtc.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();
            var shopByMonth = await _context.ShopProfiles.AsNoTracking()
                .Where(s => s.CreatedAtUtc >= windowStart)
                .GroupBy(s => new { s.CreatedAtUtc.Year, s.CreatedAtUtc.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var custLookup = customerByMonth.ToDictionary(x => (x.Year, x.Month), x => x.Count);
            var sellLookup = sellerByMonth.ToDictionary(x => (x.Year, x.Month), x => x.Count);
            var shopLookup = shopByMonth.ToDictionary(x => (x.Year, x.Month), x => x.Count);

            var userGrowth = new List<UserGrowthPointDto>(growthMonths);
            for (var i = 0; i < growthMonths; i++)
            {
                var marker = windowStart.AddMonths(i);
                var key = (marker.Year, marker.Month);
                userGrowth.Add(new UserGrowthPointDto
                {
                    Period = marker.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Label = marker.ToString("MMM", CultureInfo.InvariantCulture),
                    Customers = custLookup.TryGetValue(key, out var c) ? c : 0,
                    Sellers = sellLookup.TryGetValue(key, out var s) ? s : 0,
                    Shops = shopLookup.TryGetValue(key, out var sh) ? sh : 0,
                });
            }

            // ── App Controls strip (live config values) ──────────────────────────
            var configRows = await _context.AppRuntimeConfigs.AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new { c.Key, c.BooleanValue })
                .ToListAsync();
            var configMap = configRows.ToDictionary(c => c.Key, c => c.BooleanValue, StringComparer.OrdinalIgnoreCase);
            bool Flag(string key) => !configMap.TryGetValue(key, out var v) || v; // fail-open: default ON

            var appControls = new AnalyticsAppControlsDto
            {
                ProductPurchasingEnabled = Flag("productPurchasingEnabled"),
                ServiceBookingEnabled = Flag("serviceBookingEnabled"),
                CartAccessEnabled = Flag("cartAccessEnabled"),
                CheckoutEnabled = Flag("checkoutEnabled"),
                PaymentInitiationEnabled = Flag("paymentInitiationEnabled"),
                TestAccountAccessAllEnabled = Flag("testAccountAccessAllEnabled"),
            };

            // ── Marketplace (products) ───────────────────────────────────────────
            var avgOrderValue = await paidProductOrders.AnyAsync()
                ? Math.Round(await paidProductOrders.AverageAsync(o => o.Total), 2)
                : 0m;
            var marketplace = new AnalyticsMarketplaceDto
            {
                ActiveProductListings = await _context.Listings.AsNoTracking()
                    .CountAsync(l => l.Type == ListingType.Product && l.Status == ListingStatus.Active),
                MarketplaceListingsCreated = await _context.MarketplaceListings.AsNoTracking().CountAsync(),
                ProductOrders = productOrderCount,
                PaidProductOrders = await paidProductOrders.CountAsync(),
                OrdersAwaitingAcceptance = ordersAwaitingAcceptance,
                CompletedProductOrders = await productOrders.CountAsync(o => o.Status == OrderStatus.Completed),
                CancelledProductOrders = await productOrders.CountAsync(o => o.Status == OrderStatus.Cancelled),
                AverageOrderValue = avgOrderValue,
            };

            // ── Services (booking lifecycle, grouped in one query) ───────────────
            var bookingByStatus = await _context.ServiceBookings.AsNoTracking()
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            int BookingCount(ServiceBookingStatus st) => bookingByStatus.FirstOrDefault(x => x.Status == st)?.Count ?? 0;
            var avgBookingValue = await _context.ServiceBookings.AsNoTracking().AnyAsync()
                ? Math.Round(await _context.ServiceBookings.AsNoTracking()
                    .AverageAsync(b => b.BaseServiceAmount + b.HouseCallSurcharge + b.TravelFee), 2)
                : 0m;
            var services = new AnalyticsServicesDto
            {
                ActiveServices = await _context.Listings.AsNoTracking()
                    .CountAsync(l => l.Type == ListingType.Service && l.Status == ListingStatus.Active),
                BookingsCreated = serviceBookingCount,
                PendingBookings = BookingCount(ServiceBookingStatus.PendingPayment)
                                  + BookingCount(ServiceBookingStatus.Requested),
                ConfirmedBookings = BookingCount(ServiceBookingStatus.Confirmed)
                                    + BookingCount(ServiceBookingStatus.Accepted),
                CompletedBookings = BookingCount(ServiceBookingStatus.Completed),
                CancelledBookings = BookingCount(ServiceBookingStatus.Cancelled)
                                    + BookingCount(ServiceBookingStatus.Rejected),
                AverageBookingValue = avgBookingValue,
            };

            // ── Payments health (grouped in one query) ───────────────────────────
            var paymentByStatus = await _context.Payments.AsNoTracking()
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            int PayCount(PaymentTransactionStatus st) => paymentByStatus.FirstOrDefault(x => x.Status == st)?.Count ?? 0;
            var grossPaidAmount = await _context.Payments.AsNoTracking()
                .Where(p => p.Status == PaymentTransactionStatus.Succeeded)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;
            var paySucceeded = PayCount(PaymentTransactionStatus.Succeeded);
            var payFailed = PayCount(PaymentTransactionStatus.Failed);
            var payTerminal = paySucceeded + payFailed;
            var payments = new AnalyticsPaymentsDto
            {
                Successful = paySucceeded,
                Failed = payFailed,
                Cancelled = PayCount(PaymentTransactionStatus.Cancelled),
                Pending = PayCount(PaymentTransactionStatus.Pending) + PayCount(PaymentTransactionStatus.Initialized),
                GrossPaidAmount = grossPaidAmount,
                FailureRatePct = payTerminal > 0 ? Math.Round((decimal)payFailed / payTerminal * 100m, 1) : 0m,
            };

            // ── Dispatch / delivery health (grouped in one query) ────────────────
            var shipmentByStatus = await _context.ZansiDispatchShipments.AsNoTracking()
                .GroupBy(s => s.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();
            int ShipCount(ZansiDispatchShipmentStatus st) => shipmentByStatus.FirstOrDefault(x => x.Status == st)?.Count ?? 0;
            var dispatch = new AnalyticsDispatchDto
            {
                AwaitingSellerAcceptance = ordersAwaitingAcceptance,
                DispatchPending = ShipCount(ZansiDispatchShipmentStatus.PendingDispatch)
                                  + ShipCount(ZansiDispatchShipmentStatus.PreparingPickup),
                BookedShipments = ShipCount(ZansiDispatchShipmentStatus.BookedWithCourier),
                InTransit = ShipCount(ZansiDispatchShipmentStatus.PickedUp)
                            + ShipCount(ZansiDispatchShipmentStatus.InTransit)
                            + ShipCount(ZansiDispatchShipmentStatus.OutForDelivery),
                Delivered = ShipCount(ZansiDispatchShipmentStatus.Delivered),
                NeedsAttention = ShipCount(ZansiDispatchShipmentStatus.NeedsAttention)
                                 + ShipCount(ZansiDispatchShipmentStatus.Exception)
                                 + ShipCount(ZansiDispatchShipmentStatus.OnHold)
                                 + ShipCount(ZansiDispatchShipmentStatus.Failed),
                OrdersWithoutTracking = await _context.ZansiDispatchShipments.AsNoTracking()
                    .CountAsync(s => s.TrackingNumber == null || s.TrackingNumber == ""),
                ActualCourierCostMissing = await _context.ZansiDispatchShipments.AsNoTracking()
                    .CountAsync(s => s.Status == ZansiDispatchShipmentStatus.Delivered && s.ActualCourierCost == null),
            };

            return new AnalyticsOverviewDto
            {
                Summary = new AnalyticsSummaryDto
                {
                    TotalCustomers = totalCustomers,
                    TotalSellers = totalSellers,
                    TotalShops = totalShops,
                    ActiveListings = activeListings,
                    ProductOrders = productOrderCount,
                    ServiceBookings = serviceBookingCount,
                    GrossSales = grossSales,
                    SuccessfulPayments = successfulPayments,
                    PendingSellerActions = ordersAwaitingAcceptance + requestedBookings,
                    CancelledFailedPayments = cancelledFailedPayments,
                },
                UserGrowth = userGrowth,
                AppControls = appControls,
                Marketplace = marketplace,
                Services = services,
                Payments = payments,
                Dispatch = dispatch,
            };
        }
    }
}
