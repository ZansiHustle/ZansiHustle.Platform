namespace ZansiHustle.Shared.Enums.Listings
{
    /// <summary>
    /// How a SERVICE listing may be delivered. Applies only when
    /// <see cref="ListingType.Service"/>; product listings ignore it.
    /// </summary>
    public enum ServiceFulfilmentMode
    {
        /// <summary>Buyer travels to the provider's location only.</summary>
        ProviderLocationOnly = 1,

        /// <summary>Provider travels to the buyer's location only (house call).</summary>
        HouseCallOnly = 2,

        /// <summary>Either — buyer chooses house call or visiting the provider.</summary>
        Both = 3
    }
}
