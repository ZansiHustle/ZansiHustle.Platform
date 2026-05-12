namespace ZansiHustle.Shared.Enums.Listings;

/// <summary>
/// The origin / sales channel a listing was created under.
///
/// Listings always attach to a <c>MerchantId</c>, but a single merchant
/// can sell through multiple channels:
///   • SellerAccount — the seller is listing under their bare seller
///                     identity (no storefront branding); the buyer-
///                     facing pill shows the seller's display name.
///   • ShopProfile   — the seller chose to list under their opened
///                     ShopProfile storefront. Requires
///                     <c>Listing.ShopProfileId</c> to be set; the
///                     ShopProfile's <c>MerchantId</c> MUST equal
///                     <c>Listing.MerchantId</c> (the same merchant
///                     owns both). Buyer-facing surfaces show the
///                     shop's name as the pill, and the public
///                     ShopProfile page filters by ShopProfileId
///                     (NOT by MerchantId) so seller-account items
///                     don't bleed into the shop's catalog.
///   • PhysicalStore — listings tied to an InStoreOnly merchant
///                     (physical store catalog). ShopProfileId stays
///                     null; the StoreProfile surface drives display.
/// </summary>
public enum ListingSource
{
    SellerAccount = 1,
    ShopProfile = 2,
    PhysicalStore = 3
}
