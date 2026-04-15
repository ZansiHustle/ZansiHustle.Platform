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
        public decimal Total { get; set; }
        public string Currency { get; set; } = "ZAR";

        public string? DeliveryAddress { get; set; }
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
