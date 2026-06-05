namespace ZansiHustle.Shared.Enums.ZansiPulse;

/// <summary>
/// The kinds of user interactions ZansiPulse (the ZansiHustle intelligence
/// layer) records as <c>ZansiPulseEvent</c> rows. Stored as <c>int</c>.
///
/// Each type carries a default behavioural weight (see
/// <c>ZansiPulseDefaults.EventWeights</c>) used for interest scoring and
/// metric trending. Weights are overridable at runtime via
/// <c>ZansiPulseSetting</c> rows keyed <c>EventWeight:{EventType}</c>.
///
/// Values are explicit so reordering never silently remaps stored data.
/// </summary>
public enum ZansiPulseEventType
{
    ViewListing = 1,
    OpenListingDetail = 2,
    SearchCategory = 3,
    SearchTerm = 4,
    OpenSellerProfile = 5,
    OpenShopProfile = 6,
    FavouriteListing = 7,
    ShareListing = 8,
    MessageSeller = 9,
    OrderIntent = 10,
    HideListing = 11,
    NotInterested = 12,
    ReportListing = 13,
}
