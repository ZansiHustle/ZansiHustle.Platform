using System;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Application.Orders.Dtos
{
    public class OrderItemDto
    {
        public Guid Id { get; set; }
        public Guid? ListingId { get; set; }

        /// <summary>The specific variant purchased, if the listing sold through variants. Null otherwise.</summary>
        public Guid? VariantId { get; set; }

        /// <summary>Snapshot of the variant's name at purchase time — survives later catalog edits.</summary>
        public string? VariantName { get; set; }

        /// <summary>Snapshot of the variant's SKU at purchase time.</summary>
        public string? Sku { get; set; }

        public ListingType ListingType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
