namespace ZansiHustle.Shared.Enums.Listings;

/// <summary>
/// Where a listing is available — i.e. which surfaces are allowed to
/// surface it, and whether online checkout applies.
/// </summary>
/// <remarks>
/// <para>
/// Routes mobile / web feeds at the <see cref="Listing"/> level so that
/// physical-store catalog items (e.g. a barber's services or a retail
/// shelf product without online fulfilment) never leak into the
/// online-discovery feeds.
/// </para>
/// <list type="bullet">
///   <item>
///     <term><see cref="OnlineOnly"/></term>
///     <description>
///       Discoverable in Home / Explore / global search and via shop
///       profile. Eligible for cart / checkout / order. The default for
///       OnlineStore merchants and the migration default for all
///       pre-existing listings.
///     </description>
///   </item>
///   <item>
///     <term><see cref="InStoreOnly"/></term>
///     <description>
///       Discoverable on the merchant's Store profile (Nearby / Store
///       Locator) and on the owner's Store Dashboard only. Excluded
///       from public listing search. Not eligible for online checkout —
///       customers contact the store / get directions / WhatsApp. Only
///       valid for merchants of type <c>PhysicalStore</c>.
///     </description>
///   </item>
///   <item>
///     <term><see cref="OnlineAndInStore"/></term>
///     <description>
///       Hybrid item — appears in both online and store surfaces and
///       is eligible for online checkout. Reserved for future hybrid
///       UX; the API accepts it today but no mobile flow surfaces a
///       picker yet.
///     </description>
///   </item>
/// </list>
/// </remarks>
public enum AvailabilityMode
{
    OnlineOnly = 1,
    InStoreOnly = 2,
    OnlineAndInStore = 3
}
