using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>
    /// Buyer request to place a new order. All items must belong to the same
    /// merchant — the service resolves the merchant from the first item and
    /// rejects any mixed-merchant carts.
    /// </summary>
    public class CreateOrderRequestDto
    {
        public List<CreateOrderItemDto> Items { get; set; } = new();
        public string? DeliveryAddress { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateOrderItemDto
    {
        public Guid ListingId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
