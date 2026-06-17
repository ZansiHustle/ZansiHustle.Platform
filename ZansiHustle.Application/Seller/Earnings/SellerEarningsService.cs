using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.Seller.Earnings.Dtos;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Seller.Earnings
{
    /// <summary>
    /// Computes seller proceeds from PAID orders + service-booking lifecycle.
    /// Query-based V1 (no proceeds ledger yet) — reuses the existing seller order
    /// query + the batch booking-status lookup. Settlement model:
    ///   • Completed work (order Completed / booking Completed)      → Available
    ///   • Reversed (order Cancelled / booking Rejected|Cancelled)   → Refunds
    ///   • Everything else paid                                      → Pending
    /// No commission model exists, so PlatformFees = 0 and Net = Gross − Refunds.
    /// </summary>
    public sealed class SellerEarningsService : ISellerEarningsService
    {
        private const string Available = "Available";
        private const string Pending = "Pending";
        private const string Refunded = "Refunded";
        private const int MaxChartBuckets = 12;

        private readonly IOrderRepository _orders;
        private readonly IServiceBookingRepository _bookings;

        public SellerEarningsService(IOrderRepository orders, IServiceBookingRepository bookings)
        {
            _orders = orders;
            _bookings = bookings;
        }

        public async Task<Result<SellerEarningsSummaryDto>> GetSummaryAsync(Guid sellerUserId, string? range)
        {
            if (sellerUserId == Guid.Empty)
                return Result<SellerEarningsSummaryDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            var normRange = NormaliseRange(range);
            var nowUtc = DateTime.UtcNow;
            var fromUtc = ResolveFrom(normRange, nowUtc);

            // Only the caller's own merchants' orders (repo filters by OwnerUserId).
            var allOrders = await _orders.GetBySellerUserAsync(sellerUserId);
            var paid = allOrders
                .Where(o => o.PaymentStatus == PaymentStatus.Paid && o.CreatedAtUtc >= fromUtc)
                .ToList();

            // Settlement status for service orders (one batched query, no N+1).
            var serviceOrderIds = paid.Where(IsServiceOrder).Select(o => o.Id).ToList();
            IReadOnlyDictionary<Guid, ServiceBookingStatus> bookingStatuses =
                serviceOrderIds.Count > 0
                    ? await _bookings.GetStatusesByOrderIdsAsync(serviceOrderIds)
                    : new Dictionary<Guid, ServiceBookingStatus>();

            decimal gross = 0m, productSales = 0m, serviceSales = 0m,
                    refunds = 0m, available = 0m, pending = 0m;
            int ordersCount = 0, bookingsCount = 0;
            var currency = "ZAR";

            var topAgg = new Dictionary<string, (string type, decimal gross, int qty)>();
            var activity = new List<EarningsActivityDto>();

            foreach (var o in paid)
            {
                if (!string.IsNullOrWhiteSpace(o.Currency)) currency = o.Currency;

                var isService = IsServiceOrder(o);
                gross += o.Total;
                if (isService) { serviceSales += o.Total; bookingsCount++; }
                else { productSales += o.Total; ordersCount++; }

                var bucket = ClassifySettlement(o, isService, bookingStatuses);
                if (bucket == Refunded) refunds += o.Total;
                else if (bucket == Available) available += o.Total;
                else pending += o.Total;

                foreach (var item in o.Items)
                {
                    var key = string.IsNullOrWhiteSpace(item.TitleSnapshot) ? "Item" : item.TitleSnapshot;
                    var type = item.ListingType == ListingType.Service ? "Service" : "Product";
                    if (!topAgg.TryGetValue(key, out var agg)) agg = (type, 0m, 0);
                    topAgg[key] = (agg.type, agg.gross + item.LineTotal, agg.qty + Math.Max(1, item.Quantity));
                }

                activity.Add(new EarningsActivityDto
                {
                    Id = o.Id,
                    Type = isService ? "Service" : "Product",
                    Title = o.Items.FirstOrDefault()?.TitleSnapshot ?? (o.Merchant?.Name ?? "Order"),
                    Amount = o.Total,
                    Status = bucket,
                    OccurredAtUtc = o.CreatedAtUtc
                });
            }

            const decimal platformFees = 0m; // no commission model yet
            var net = gross - platformFees - refunds;
            var totalCount = ordersCount + bookingsCount;
            var aov = totalCount > 0 ? Math.Round(gross / totalCount, 2, MidpointRounding.AwayFromZero) : 0m;

            var dto = new SellerEarningsSummaryDto
            {
                Range = normRange,
                Currency = currency,
                GrossSales = gross,
                ProductSales = productSales,
                ServiceSales = serviceSales,
                PlatformFees = platformFees,
                Refunds = refunds,
                NetProceeds = net,
                PendingSettlement = pending,
                AvailableForPayout = available,
                PaidOut = 0m,
                OrdersCount = ordersCount,
                BookingsCount = bookingsCount,
                AverageOrderValue = aov,
                ChartPoints = BuildChart(paid, normRange, fromUtc, nowUtc, bookingStatuses),
                TopItems = topAgg
                    .OrderByDescending(x => x.Value.gross)
                    .Take(5)
                    .Select(x => new EarningsTopItemDto
                    {
                        Type = x.Value.type,
                        Title = x.Key,
                        QuantityOrBookings = x.Value.qty,
                        GrossSales = x.Value.gross,
                        NetProceeds = x.Value.gross // fees 0 — never overstated
                    })
                    .ToList(),
                RecentActivity = activity
                    .OrderByDescending(a => a.OccurredAtUtc)
                    .Take(10)
                    .ToList(),
                // No managed payout flow yet — client shows the portal message.
                PayoutSelfServiceAvailable = false
            };

            return Result<SellerEarningsSummaryDto>.Success(dto, "Earnings summary computed.");
        }

        public async Task<Result<SellerFinanceSummaryDto>> GetFinanceSummaryAsync(Guid sellerUserId)
        {
            if (sellerUserId == Guid.Empty)
                return Result<SellerFinanceSummaryDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            // ALL-TIME, PAID orders for the caller's own merchants only.
            var allOrders = await _orders.GetBySellerUserAsync(sellerUserId);
            var paid = allOrders.Where(o => o.PaymentStatus == PaymentStatus.Paid).ToList();

            var serviceOrderIds = paid.Where(IsServiceOrder).Select(o => o.Id).ToList();
            IReadOnlyDictionary<Guid, ServiceBookingStatus> bookingStatuses =
                serviceOrderIds.Count > 0
                    ? await _bookings.GetStatusesByOrderIdsAsync(serviceOrderIds)
                    : new Dictionary<Guid, ServiceBookingStatus>();

            // Total earned = completed/eligible work only (the "Available" bucket):
            // cancelled/refunded never count; paid-but-unfulfilled is NOT yet earned.
            // Fees = 0 (no commission model), so net == gross of eligible orders.
            decimal totalEarned = 0m;
            var currency = "ZAR";
            foreach (var o in paid)
            {
                if (!string.IsNullOrWhiteSpace(o.Currency)) currency = o.Currency;
                if (ClassifySettlement(o, IsServiceOrder(o), bookingStatuses) == Available)
                    totalEarned += o.Total;
            }

            // TODO(seller-payouts): there is no seller withdrawal/payout flow yet, so
            // nothing has actually been paid out. Honest zero (never faked). When a
            // payout ledger exists, sum COMPLETED payouts here.
            const decimal paidOut = 0m;
            var pendingPayout = totalEarned - paidOut;
            if (pendingPayout < 0m) pendingPayout = 0m;

            return Result<SellerFinanceSummaryDto>.Success(new SellerFinanceSummaryDto
            {
                TotalEarned = totalEarned,
                PaidOut = paidOut,
                PendingPayout = pendingPayout,
                Currency = currency,
            }, "Seller finance summary computed.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static bool IsServiceOrder(Order o) =>
            o.Items.Any(i => i.ListingType == ListingType.Service);

        /// <summary>Settlement bucket for a PAID order: Available (completed work),
        /// Refunded (reversed), or Pending (in progress / awaiting).</summary>
        private static string ClassifySettlement(
            Order o, bool isService, IReadOnlyDictionary<Guid, ServiceBookingStatus> bookingStatuses)
        {
            if (isService)
            {
                var status = bookingStatuses.TryGetValue(o.Id, out var s) ? s : ServiceBookingStatus.Requested;
                if (status == ServiceBookingStatus.Rejected || status == ServiceBookingStatus.Cancelled)
                    return Refunded;
                if (status == ServiceBookingStatus.Completed)
                    return Available;
                return Pending;
            }

            if (o.Status == OrderStatus.Cancelled) return Refunded;
            if (o.Status == OrderStatus.Completed) return Available;
            return Pending;
        }

        private static List<EarningsChartPointDto> BuildChart(
            List<Order> paid, string range, DateTime fromUtc, DateTime nowUtc,
            IReadOnlyDictionary<Guid, ServiceBookingStatus> bookingStatuses)
        {
            if (paid.Count == 0) return new List<EarningsChartPointDto>();

            var start = range == "all" ? paid.Min(o => o.CreatedAtUtc).Date : fromUtc;
            var end = nowUtc;
            var totalDays = Math.Max(1, (int)Math.Ceiling((end - start).TotalDays));
            var bucketCount = Math.Max(1, Math.Min(MaxChartBuckets, totalDays));
            var bucketDays = Math.Max(1, (int)Math.Ceiling((double)totalDays / bucketCount));

            var points = new List<EarningsChartPointDto>();
            for (var i = 0; i < bucketCount; i++)
            {
                var bStart = start.AddDays((double)i * bucketDays);
                var bEnd = i == bucketCount - 1 ? end.AddDays(1) : start.AddDays((double)(i + 1) * bucketDays);

                decimal g = 0m, refund = 0m;
                var count = 0;
                foreach (var o in paid)
                {
                    if (o.CreatedAtUtc < bStart || o.CreatedAtUtc >= bEnd) continue;
                    g += o.Total;
                    count++;
                    if (ClassifySettlement(o, IsServiceOrder(o), bookingStatuses) == Refunded)
                        refund += o.Total;
                }

                points.Add(new EarningsChartPointDto
                {
                    Date = bStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    GrossSales = g,
                    NetProceeds = g - refund,
                    OrdersCount = count
                });
            }
            return points;
        }

        private static string NormaliseRange(string? range)
        {
            var r = (range ?? string.Empty).Trim().ToLowerInvariant();
            return r switch
            {
                "7d" or "30d" or "90d" or "month" or "all" => r,
                _ => "30d"
            };
        }

        private static DateTime ResolveFrom(string range, DateTime nowUtc) => range switch
        {
            "7d" => nowUtc.AddDays(-7),
            "30d" => nowUtc.AddDays(-30),
            "90d" => nowUtc.AddDays(-90),
            "month" => new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            "all" => DateTime.MinValue,
            _ => nowUtc.AddDays(-30)
        };
    }
}
