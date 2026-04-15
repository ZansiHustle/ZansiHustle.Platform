using System;
using System.Collections.Generic;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Shared.Enums.Orders;

namespace ZansiHustle.Domain.Orders
{
    /// <summary>
    /// A marketplace order placed by a buyer against a single merchant (shop).
    /// v1 constraint: each order belongs to exactly one merchant — buyers who
    /// checkout across multiple shops create multiple orders.
    /// </summary>
    public class Order
    {
        public Guid Id { get; set; }

        /// <summary>Short opaque business code (e.g. <c>ORD-20260415...</c>).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Buyer's ASP.NET Identity user id.</summary>
        public Guid BuyerUserId { get; set; }

        /// <summary>Buyer display snapshot — captured at placement so the seller
        /// view keeps context even if the buyer later renames themselves.</summary>
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }

        /// <summary>Owning merchant. Required; cascades delete.</summary>
        public Guid MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = "ZAR";

        /// <summary>Optional delivery/fulfilment hints; v1 plain-text only.</summary>
        public string? DeliveryAddress { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? ConfirmedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }
        public string? CancellationReason { get; set; }

        public List<OrderItem> Items { get; set; } = new();
    }
}
