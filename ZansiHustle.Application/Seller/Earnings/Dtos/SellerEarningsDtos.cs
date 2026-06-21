using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Seller.Earnings.Dtos
{
    /// <summary>
    /// Seller-facing earnings summary for a time range. Computed server-side from
    /// PAID orders + service-booking lifecycle (no commission model yet → fees are
    /// 0). This is SELLER PROCEEDS — deliberately separate from the customer
    /// refund/spend wallet (that's ZansiHustle.Application.Wallets). We never call
    /// any figure "profit" (seller costs are unknown).
    /// </summary>
    public class SellerEarningsSummaryDto
    {
        public string Range { get; set; } = "30d";
        public string Currency { get; set; } = "ZAR";

        /// <summary>Total value of paid sales in the range (GMV = sum of Order.Total).</summary>
        public decimal GrossSales { get; set; }
        public decimal ProductSales { get; set; }
        public decimal ServiceSales { get; set; }
        /// <summary>Fee base = sum of Order.Subtotal (EXCLUDES delivery). The amount
        /// the 3% gateway and 5% platform fees are computed against.</summary>
        public decimal EligibleSales { get; set; }
        /// <summary>Gateway (Ozow) fees withheld — 3% of eligible sales. A cost,
        /// not platform profit.</summary>
        public decimal GatewayFees { get; set; }
        /// <summary>ZansiHustle platform fees withheld — now real 5% of eligible sales.</summary>
        public decimal PlatformFees { get; set; }
        /// <summary>Delivery/courier fees excluded from earnings (sum of Order.DeliveryFee).
        /// Pass-through — never seller earnings.</summary>
        public decimal DeliveryExcluded { get; set; }
        /// <summary>Paid sales that were later reversed (rejected/cancelled bookings,
        /// cancelled orders). Not part of available/pending.</summary>
        public decimal Refunds { get; set; }
        /// <summary>EligibleSales − GatewayFees − PlatformFees − Refunds.</summary>
        public decimal NetProceeds { get; set; }

        /// <summary>Net of in-progress work — clears once the order/booking completes.</summary>
        public decimal PendingSettlement { get; set; }
        /// <summary>Net of completed work — eligible for payout.</summary>
        public decimal AvailableForPayout { get; set; }
        /// <summary>Already paid out to the seller. 0 — no payout ledger yet (not faked).</summary>
        public decimal PaidOut { get; set; }

        public int OrdersCount { get; set; }
        public int BookingsCount { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<EarningsChartPointDto> ChartPoints { get; set; } = new();
        public List<EarningsTopItemDto> TopItems { get; set; } = new();
        public List<EarningsActivityDto> RecentActivity { get; set; } = new();

        /// <summary>False = no in-app payout; the client shows "payouts handled from
        /// the seller portal" instead of a Request-payout button (no fake payouts).</summary>
        public bool PayoutSelfServiceAvailable { get; set; }
    }

    public class EarningsChartPointDto
    {
        /// <summary>Bucket start date (yyyy-MM-dd, UTC — the client formats to SAST).</summary>
        public string Date { get; set; } = string.Empty;
        public decimal GrossSales { get; set; }
        public decimal NetProceeds { get; set; }
        public int OrdersCount { get; set; }
    }

    public class EarningsTopItemDto
    {
        public string Type { get; set; } = "Product"; // "Product" | "Service"
        public string Title { get; set; } = string.Empty;
        public int QuantityOrBookings { get; set; }
        public decimal GrossSales { get; set; }
        public decimal NetProceeds { get; set; }
    }

    public class EarningsActivityDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "Product"; // "Product" | "Service"
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        /// <summary>"Available" | "Pending" | "Refunded".</summary>
        public string Status { get; set; } = string.Empty;
        public DateTime OccurredAtUtc { get; set; }
    }
}
