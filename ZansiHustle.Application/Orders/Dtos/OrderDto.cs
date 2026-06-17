using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Orders;

namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>Full order detail DTO.</summary>
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;

        public Guid BuyerUserId { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerEmail { get; set; }
        public string? BuyerPhone { get; set; }

        public Guid MerchantId { get; set; }
        public string? MerchantName { get; set; }
        public string? MerchantSlug { get; set; }
        public string? MerchantLogoUrl { get; set; }

        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public decimal Subtotal { get; set; }
        /// <summary>ZansiDispatch delivery fee included in <see cref="Total"/>. Null = no managed delivery.</summary>
        public decimal? DeliveryFee { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = "ZAR";

        // ── Payment composition (wallet-as-payment). 0/null when no wallet used. ──
        /// <summary>Wallet balance applied toward this order.</summary>
        public decimal WalletAmountApplied { get; set; }
        /// <summary>Amount charged/charged-due via the external gateway (Total − wallet).</summary>
        public decimal? ExternalAmountDue { get; set; }

        public string? DeliveryAddress { get; set; }

        /// <summary>
        /// Broad delivery AREA only (e.g. "Hatfield, Pretoria") — suburb + city,
        /// never the street line, postal code, province, or coordinates. Populated
        /// for the SELLER's view of a product order so they have logistics context
        /// WITHOUT the buyer's full address. Null on the buyer's own view (they see
        /// their full <see cref="DeliveryAddress"/>).
        /// </summary>
        public string? BuyerDeliveryArea { get; set; }

        public string? Notes { get; set; }
        public string? CancellationReason { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public DateTime? ConfirmedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime? CancelledAtUtc { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
    }
}
