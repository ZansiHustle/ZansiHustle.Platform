using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Request to attach EXISTING own listings to a shop storefront.
    /// Product + service ids are kept separate to match the client UI but the
    /// service treats them uniformly (a Listing is product OR service). Either
    /// list may be empty/omitted.
    /// </summary>
    public class AssignShopItemsRequestDto
    {
        public List<Guid>? ProductIds { get; set; }
        public List<Guid>? ServiceIds { get; set; }
    }
}
