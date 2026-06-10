namespace ZansiHustle.Shared.Enums.ServiceBookings;

/// <summary>
/// The concrete delivery mode a buyer chose for a single booking. This is the
/// resolved binary choice (one slot, one mode), distinct from the listing-level
/// <see cref="ZansiHustle.Shared.Enums.Listings.ServiceFulfilmentMode"/> which
/// describes which modes the service OFFERS (Both / HouseCallOnly / …).
/// </summary>
public enum ServiceBookingMode
{
    /// <summary>Provider travels to the buyer's address.</summary>
    HouseCall = 1,

    /// <summary>Buyer visits the provider's business location.</summary>
    ProviderLocation = 2
}
