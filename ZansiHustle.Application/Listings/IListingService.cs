using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Listings
{
    /// <summary>
    /// Service contract for listing (product + service) operations.
    /// Write operations enforce shop ownership via the caller's user id.
    /// </summary>
    public interface IListingService
    {
        Task<Result<PagedResult<ListingListItemDto>>> SearchAsync(ListingFilterRequestDto filter);
        Task<Result<ListingDto>> GetByIdAsync(Guid id);
        Task<Result<List<ListingListItemDto>>> GetByMerchantAsync(Guid merchantId);

        /// <summary>
        /// Returns listings explicitly attached to a ShopProfile.
        /// Used by the public ShopProfile catalog so SellerAccount
        /// items under the same merchant don't bleed into the shop.
        /// </summary>
        Task<Result<List<ListingListItemDto>>> GetByShopProfileAsync(Guid shopProfileId);

        Task<Result<List<ListingListItemDto>>> GetMineAsync(Guid ownerUserId);
        Task<Result<ListingDto>> CreateAsync(Guid ownerUserId, CreateListingRequestDto request);
        Task<Result<ListingDto>> UpdateAsync(Guid ownerUserId, Guid listingId, UpdateListingRequestDto request);
        Task<Result> DeleteAsync(Guid ownerUserId, Guid listingId);

        /// <summary>
        /// Attaches the caller's EXISTING listings to one of their shops
        /// (sets ListingSource=ShopProfile + ShopProfileId). Idempotent;
        /// enforces that each listing is owned by the caller and belongs to the
        /// shop's merchant. Never duplicates or moves another seller's items.
        /// </summary>
        Task<Result<AssignShopItemsResultDto>> AssignToShopAsync(
            Guid ownerUserId, Guid shopProfileId, IReadOnlyCollection<Guid> listingIds);
    }
}
