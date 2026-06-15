using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.CustomerFinance.Dtos;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.Wallets;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.Wallets;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.Wallets;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.CustomerFinance
{
    /// <summary>
    /// Builds the customer finance overview by combining the buyer's orders
    /// (the unit of spend, so a wallet+external split is counted ONCE) with the
    /// wallet ledger (refunds, withdrawals, adjustments) and the wallet balance.
    /// The internal <c>WalletPaymentDebit</c> ledger row is excluded from the
    /// list — it's already represented by its order — which is what prevents the
    /// double-count.
    /// </summary>
    public sealed class CustomerFinanceService : ICustomerFinanceService
    {
        private readonly IOrderRepository _orders;
        private readonly IWalletRepository _wallets;
        private readonly ILogger<CustomerFinanceService> _logger;

        public CustomerFinanceService(
            IOrderRepository orders, IWalletRepository wallets, ILogger<CustomerFinanceService> logger)
        {
            _orders = orders;
            _wallets = wallets;
            _logger = logger;
        }

        private static readonly WalletTransactionType[] RefundTypes =
        {
            WalletTransactionType.BookingRejectedCredit,
            WalletTransactionType.OrderCancelledCredit,
            WalletTransactionType.RefundCredit,
            WalletTransactionType.BookingCancelledCredit,
        };

        public async Task<Result<FinanceSummaryDto>> GetSummaryAsync(Guid userId)
        {
            try
            {
                var orders = await _orders.GetByBuyerAsync(userId);
                var walletTxns = await _wallets.GetAllTransactionsAsync(userId);
                var wallet = await _wallets.GetByUserAsync(userId);

                var now = DateTime.UtcNow;
                var weekAgo = now.AddDays(-7);
                var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                // Money was actually charged for orders that are Paid OR Refunded
                // (refunded = paid then returned). Pending/Failed never charged.
                var charged = orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Paid || o.PaymentStatus == PaymentStatus.Refunded)
                    .ToList();

                decimal grossAll = charged.Sum(o => o.Total);
                decimal walletSpent = charged.Sum(o => o.WalletAmountApplied);
                decimal externalSpent = charged.Sum(o => ExternalPortion(o));
                decimal productSpend = charged.Where(o => !IsServiceOrder(o)).Sum(o => o.Total);
                decimal serviceSpend = charged.Where(IsServiceOrder).Sum(o => o.Total);
                decimal spentThisWeek = charged.Where(o => o.CreatedAtUtc >= weekAgo).Sum(o => o.Total);
                decimal spentThisMonth = charged.Where(o => o.CreatedAtUtc >= monthStart).Sum(o => o.Total);

                decimal totalRefunded = walletTxns
                    .Where(t => t.Status == WalletTransactionStatus.Completed
                        && t.Direction == WalletTransactionDirection.Credit
                        && RefundTypes.Contains(t.Type))
                    .Sum(t => t.Amount);

                var currency = wallet?.Currency
                    ?? charged.FirstOrDefault()?.Currency
                    ?? "ZAR";

                var combinedCount = BuildCombined(orders, walletTxns, currency).Count;

                var dto = new FinanceSummaryDto
                {
                    WalletBalance = wallet?.AvailableBalance ?? 0m,
                    SpentThisWeek = spentThisWeek,
                    SpentThisMonth = spentThisMonth,
                    SpentAllTime = grossAll,
                    TotalSpent = grossAll - totalRefunded, // net all-time
                    TotalRefunded = totalRefunded,
                    WalletSpent = walletSpent,
                    ExternalSpent = externalSpent,
                    ProductSpend = productSpend,
                    ServiceSpend = serviceSpend,
                    TransactionCount = combinedCount,
                    Currency = currency,
                };

                return Result<FinanceSummaryDto>.Success(dto, "Finance summary retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build finance summary for user {UserId}.", userId);
                return Result<FinanceSummaryDto>.Failure(ErrorCodes.Exception, "Could not load your spending summary.");
            }
        }

        public async Task<Result<FinanceTransactionsPageDto>> GetTransactionsAsync(
            Guid userId, string? type, string? range, int page, int pageSize)
        {
            try
            {
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 30 : (pageSize > 100 ? 100 : pageSize);

                var orders = await _orders.GetByBuyerAsync(userId);
                var walletTxns = await _wallets.GetAllTransactionsAsync(userId);
                var wallet = await _wallets.GetByUserAsync(userId);
                var currency = wallet?.Currency ?? orders.FirstOrDefault()?.Currency ?? "ZAR";

                IEnumerable<FinanceTransactionDto> all = BuildCombined(orders, walletTxns, currency);

                // Range filter.
                var from = RangeStart(range, DateTime.UtcNow);
                if (from > DateTime.MinValue)
                    all = all.Where(t => t.OccurredAtUtc >= from);

                // Type filter.
                all = (type?.Trim().ToLowerInvariant()) switch
                {
                    "orders" => all.Where(t => t.Type == "ProductOrderPayment"),
                    "bookings" => all.Where(t => t.Type == "ServiceBookingPayment"),
                    "refunds" => all.Where(t => t.IsRefund),
                    "wallet" => all.Where(t => t.RelatedWalletTransactionId != null),
                    _ => all,
                };

                var ordered = all.OrderByDescending(t => t.OccurredAtUtc).ToList();
                var total = ordered.Count;
                var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                var dto = new FinanceTransactionsPageDto
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    Total = total,
                    HasMore = page * pageSize < total,
                    Currency = currency,
                };

                return Result<FinanceTransactionsPageDto>.Success(dto, "Finance transactions retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list finance transactions for user {UserId}.", userId);
                return Result<FinanceTransactionsPageDto>.Failure(ErrorCodes.Exception, "Could not load your transactions.");
            }
        }

        // ── Combined builder ─────────────────────────────────────────────────
        private static List<FinanceTransactionDto> BuildCombined(
            List<Order> orders, List<WalletTransaction> walletTxns, string fallbackCurrency)
        {
            var list = new List<FinanceTransactionDto>();

            // 1) Order payments (charged orders). One row per order = the spend
            //    unit, so a wallet+external split is counted ONCE.
            foreach (var o in orders)
            {
                if (o.PaymentStatus != PaymentStatus.Paid && o.PaymentStatus != PaymentStatus.Refunded)
                    continue;

                var isService = IsServiceOrder(o);
                var firstTitle = o.Items.FirstOrDefault()?.TitleSnapshot;
                var statusText = o.PaymentStatus == PaymentStatus.Refunded ? "Refunded"
                    : o.Status == OrderStatus.AwaitingSellerAcceptance ? "Awaiting seller confirmation"
                    : o.Status == OrderStatus.Completed ? "Completed"
                    : "Paid";

                list.Add(new FinanceTransactionDto
                {
                    Id = $"order:{o.Id}",
                    OccurredAtUtc = o.CreatedAtUtc,
                    Type = isService ? "ServiceBookingPayment" : "ProductOrderPayment",
                    Direction = "Debit",
                    Amount = o.Total,
                    Currency = string.IsNullOrWhiteSpace(o.Currency) ? fallbackCurrency : o.Currency,
                    Title = isService ? (firstTitle ?? "Service booking") : (firstTitle ?? "Order"),
                    Description = $"Order {o.Code}",
                    Reference = o.Code,
                    Status = statusText,
                    RelatedOrderId = o.Id,
                    PaymentMethod = PaymentMethodFor(o),
                    IsRefund = false,
                    IsPending = o.Status == OrderStatus.AwaitingSellerAcceptance,
                });
            }

            // 2) Wallet ledger — refunds, reversals, withdrawals, adjustments.
            //    Exclude WalletPaymentDebit (already represented by its order) and
            //    WithdrawalPaid (a payout completion, no balance change).
            foreach (var t in walletTxns)
            {
                if (t.Type == WalletTransactionType.WalletPaymentDebit) continue;
                if (t.Type == WalletTransactionType.WithdrawalPaid) continue;

                var isCredit = t.Direction == WalletTransactionDirection.Credit;
                var isRefund = RefundTypes.Contains(t.Type);
                string ftype = isRefund ? "Refund"
                    : t.Type == WalletTransactionType.WalletPaymentReversal ? "PaymentReversal"
                    : t.Type == WalletTransactionType.WithdrawalRequested ? "Withdrawal"
                    : isCredit ? "WalletCredit" : "WalletDebit";

                string title = ftype switch
                {
                    "Refund" => "Refund",
                    "PaymentReversal" => "Wallet payment reversed",
                    "Withdrawal" => "Withdrawal",
                    "WalletCredit" => "Wallet credit",
                    _ => "Wallet debit",
                };

                list.Add(new FinanceTransactionDto
                {
                    Id = $"wallet:{t.Id}",
                    OccurredAtUtc = t.CreatedAtUtc,
                    Type = ftype,
                    Direction = isCredit ? "Credit" : "Debit",
                    Amount = t.Amount,
                    Currency = string.IsNullOrWhiteSpace(t.Currency) ? fallbackCurrency : t.Currency,
                    Title = title,
                    Description = t.Description,
                    Reference = t.ReferenceType,
                    Status = t.Status.ToString(),
                    RelatedOrderId = string.Equals(t.ReferenceType, "Order", StringComparison.OrdinalIgnoreCase) ? t.ReferenceId : null,
                    RelatedBookingId = string.Equals(t.ReferenceType, "ServiceBooking", StringComparison.OrdinalIgnoreCase) ? t.ReferenceId : null,
                    RelatedWalletTransactionId = t.Id,
                    PaymentMethod = "Wallet",
                    IsRefund = isRefund,
                    IsPending = t.Status == WalletTransactionStatus.Pending,
                });
            }

            return list;
        }

        private static bool IsServiceOrder(Order o) =>
            o.Items.Any(i => i.ListingType == ListingType.Service);

        private static decimal ExternalPortion(Order o) =>
            o.ExternalAmountDue ?? (o.Total - o.WalletAmountApplied);

        private static string PaymentMethodFor(Order o)
        {
            var ext = ExternalPortion(o);
            if (o.WalletAmountApplied > 0m && ext > 0m) return "WalletPlusOzow";
            if (o.WalletAmountApplied > 0m) return "Wallet";
            if (ext > 0m) return "Ozow";
            return "Unknown";
        }

        private static DateTime RangeStart(string? range, DateTime now) =>
            (range?.Trim().ToLowerInvariant()) switch
            {
                "7d" => now.AddDays(-7),
                "30d" => now.AddDays(-30),
                "month" => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                "year" => new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                _ => DateTime.MinValue,
            };
    }
}
