namespace ZansiHustle.Application.Communications.Email.Models;

/// <summary>
/// Everything the "shipment booked" emails need. Built by ZansiDispatch after a
/// successful courier booking. Privacy is enforced by WHICH fields each template
/// reads: the SELLER template uses <see cref="BuyerName"/> + <see cref="DeliveryArea"/>
/// only (never the buyer email/phone/full address); the CUSTOMER template uses
/// the buyer's own full delivery address.
/// </summary>
public sealed class ShipmentEmailContext
{
    public string OrderCode { get; set; } = string.Empty;

    // Recipients (resolved real addresses; may be overridden in test mode).
    public string? CustomerEmail { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? SellerEmail { get; set; }
    public string? SellerName { get; set; }
    public string? SellerPhone { get; set; }

    // Shipment facts.
    public string? TrackingReference { get; set; }
    public string? Courier { get; set; }
    public string? ServiceLevel { get; set; }
    public string? Status { get; set; }

    // Courier date promises (date-only; already provider-confirmed or null).
    public System.DateTime? ExpectedCollectionDate { get; set; }
    public System.DateTime? ExpectedDeliveryFrom { get; set; }
    public System.DateTime? ExpectedDeliveryTo { get; set; }

    // Addresses — customer sees the full delivery address (their own); seller
    // sees the pickup address + a BROAD delivery area only.
    public string? DeliveryAddressFull { get; set; }
    public string? DeliveryArea { get; set; }
    public string? PickupAddress { get; set; }
}
