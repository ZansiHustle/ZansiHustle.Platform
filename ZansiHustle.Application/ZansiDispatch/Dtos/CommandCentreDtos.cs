using System;

namespace ZansiHustle.Application.ZansiDispatch.Dtos
{
    /// <summary>
    /// CEO/ops overview for the ZansiDispatch command centre. Money totals plus
    /// shipment-state counts for a period.
    /// </summary>
    public sealed class CommandCentreOverviewDto
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        public decimal TotalQuotedDeliveryFees { get; set; }
        public decimal TotalActualCourierCosts { get; set; }
        public decimal TotalSurplus { get; set; }
        public decimal TotalDeficit { get; set; }
        /// <summary>Net logistics balance = surplus − deficit (over captured shipments).</summary>
        public decimal NetLogisticsBalance { get; set; }

        public int ShipmentsPendingDispatch { get; set; }
        /// <summary>Shipments booked with a courier (status BookedWithCourier).</summary>
        public int ShipmentsBookedWithCourier { get; set; }
        public int ShipmentsInTransit { get; set; }
        public int ShipmentsDelivered { get; set; }
        /// <summary>Shipments in an exception/failed state (Failed + Exception).</summary>
        public int ShipmentsExceptions { get; set; }
        public int ShipmentsPendingReconciliation { get; set; }
        public int ShipmentsTotal { get; set; }

        /// <summary>Failed provider calls (any operation) in the period.</summary>
        public int ProviderFailureCount { get; set; }
        /// <summary>Quotes where the courier couldn't price and InternalEstimate was used.</summary>
        public int FallbackQuoteCount { get; set; }
    }

    /// <summary>
    /// Resolved + validated quote option used by the order-creation flow to set
    /// the delivery fee and build the shipment. Returned by the dispatch
    /// service so OrderService never touches ZansiDispatch entities directly.
    /// </summary>
    public sealed class SelectableQuoteOptionDto
    {
        public Guid QuoteId { get; set; }
        public Guid QuoteOptionId { get; set; }
        public Shared.Enums.ZansiDispatch.ZansiDispatchProviderType ProviderType { get; set; }
        public Shared.Enums.ZansiDispatch.ZansiDispatchServiceLevel ServiceLevel { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public Guid? MerchantId { get; set; }
        public Guid? ShopId { get; set; }
        public string? PickupAddressSummary { get; set; }
        public string? DropoffAddressSummary { get; set; }
        /// <summary>Buyer (delivery) postal code captured on the quote — required
        /// before a dispatchable order can be paid for.</summary>
        public string? BuyerPostalCode { get; set; }
    }
}
