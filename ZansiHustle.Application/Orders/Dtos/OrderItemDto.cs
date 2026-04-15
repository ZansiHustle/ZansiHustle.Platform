using System;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.Orders.Dtos
{
    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid? ListingId { get; set; }
        public ListingType ListingType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
