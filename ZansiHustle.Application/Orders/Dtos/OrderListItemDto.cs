using System;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Orders;

namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>Lightweight order summary used in list endpoints.</summary>
    public class OrderListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;

        public Guid BuyerUserId { get; set; }
        public string? BuyerName { get; set; }

        public Guid MerchantId { get; set; }
        public string? MerchantName { get; set; }

        public OrderStatus Status { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        public decimal Total { get; set; }
        public string Currency { get; set; } = "ZAR";

        public int ItemCount { get; set; }
        public string? FirstItemTitle { get; set; }
        public string? FirstItemImageUrl { get; set; }

        /// <summary>Type of the first item; mobile uses this to split products vs services in list UIs.</summary>
        public ListingType? FirstItemListingType { get; set; }

        /// <summary>
        /// For a SERVICE order: the booking lifecycle status name
        /// (Requested/Accepted/InProgress/Completed/Rejected/Cancelled), with
        /// legacy Confirmed normalised to "Requested". Null for product orders
        /// (and service orders without a booking row). Lets the list show the
        /// true booking status instead of the misleading order-level "Confirmed".
        /// </summary>
        public string? ServiceBookingStatus { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
