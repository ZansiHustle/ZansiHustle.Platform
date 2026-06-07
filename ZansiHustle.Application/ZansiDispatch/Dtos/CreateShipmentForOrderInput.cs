using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Application.ZansiDispatch.Dtos
{
    /// <summary>
    /// Input the order-creation flow hands to ZansiDispatch (post-save) to spin
    /// up the shipment for an order. Passing primitives — not domain entities —
    /// keeps the Orders module decoupled from ZansiDispatch internals.
    /// </summary>
    public sealed class CreateShipmentForOrderInput
    {
        public Guid OrderId { get; set; }
        public Guid UserId { get; set; }
        public Guid? MerchantId { get; set; }
        public Guid? ShopId { get; set; }

        public Guid QuoteId { get; set; }
        public Guid QuoteOptionId { get; set; }
        public ZansiDispatchProviderType ProviderType { get; set; }
        public ZansiDispatchServiceLevel ServiceLevel { get; set; }
        public decimal QuotedDeliveryFee { get; set; }

        public string? PickupAddressSummary { get; set; }
        public string? DropoffAddressSummary { get; set; }
    }
}
