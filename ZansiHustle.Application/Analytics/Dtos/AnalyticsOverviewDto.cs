using System.Collections.Generic;

namespace ZansiHustle.Application.Analytics.Dtos
{
    /// <summary>
    /// Live command-center snapshot for the portal Analytics page. Every figure
    /// is an aggregate COUNT/SUM straight from the DB — no PII, no fabricated
    /// values. Anything the schema can't yet separate is returned as 0 and
    /// labelled, never faked.
    /// </summary>
    public sealed class AnalyticsOverviewDto
    {
        public AnalyticsSummaryDto Summary { get; set; } = new();
        public List<UserGrowthPointDto> UserGrowth { get; set; } = new();
        public AnalyticsAppControlsDto AppControls { get; set; } = new();
        public AnalyticsMarketplaceDto Marketplace { get; set; } = new();
        public AnalyticsServicesDto Services { get; set; } = new();
        public AnalyticsPaymentsDto Payments { get; set; } = new();
        public AnalyticsDispatchDto Dispatch { get; set; } = new();
    }

    public sealed class AnalyticsSummaryDto
    {
        public int TotalCustomers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalShops { get; set; }
        public int ActiveListings { get; set; }
        public int ProductOrders { get; set; }
        public int ServiceBookings { get; set; }
        public decimal GrossSales { get; set; }
        public int SuccessfulPayments { get; set; }
        public int PendingSellerActions { get; set; }
        public int CancelledFailedPayments { get; set; }
    }

    /// <summary>New registrations per calendar month. "Do not invent growth" —
    /// every month in the window is emitted with real counts (zeros included).</summary>
    public sealed class UserGrowthPointDto
    {
        public string Period { get; set; } = string.Empty; // yyyy-MM
        public string Label { get; set; } = string.Empty;  // e.g. "Jun"
        public int Customers { get; set; }
        public int Sellers { get; set; }
        public int Shops { get; set; }
    }

    public sealed class AnalyticsAppControlsDto
    {
        public bool ProductPurchasingEnabled { get; set; } = true;
        public bool ServiceBookingEnabled { get; set; } = true;
        public bool CartAccessEnabled { get; set; } = true;
        public bool CheckoutEnabled { get; set; } = true;
        public bool PaymentInitiationEnabled { get; set; } = true;
        public bool TestAccountAccessAllEnabled { get; set; } = true;
    }

    public sealed class AnalyticsMarketplaceDto
    {
        public int ActiveProductListings { get; set; }
        public int MarketplaceListingsCreated { get; set; }
        public int ProductOrders { get; set; }
        public int PaidProductOrders { get; set; }
        public int OrdersAwaitingAcceptance { get; set; }
        public int CompletedProductOrders { get; set; }
        public int CancelledProductOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public sealed class AnalyticsServicesDto
    {
        public int ActiveServices { get; set; }
        public int BookingsCreated { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal AverageBookingValue { get; set; }
    }

    public sealed class AnalyticsPaymentsDto
    {
        public int Successful { get; set; }
        public int Failed { get; set; }
        public int Cancelled { get; set; }
        public int Pending { get; set; }
        public decimal GrossPaidAmount { get; set; }
        public decimal FailureRatePct { get; set; }
    }

    public sealed class AnalyticsDispatchDto
    {
        public int AwaitingSellerAcceptance { get; set; }
        public int DispatchPending { get; set; }
        public int BookedShipments { get; set; }
        public int InTransit { get; set; }
        public int Delivered { get; set; }
        public int NeedsAttention { get; set; }
        public int OrdersWithoutTracking { get; set; }
        public int ActualCourierCostMissing { get; set; }
    }
}
