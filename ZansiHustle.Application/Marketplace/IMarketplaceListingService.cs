using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Marketplace.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Marketplace
{
    /// <summary>
    /// Application-layer surface for the casual peer-to-peer Marketplace.
    ///
    /// Reads are public; writes are scoped to the authenticated user via
    /// the JWT and an owner check inside the service. Marketplace must
    /// not be coupled to the merchant/Listings/Order/Payment pipeline —
    /// buyers contact sellers directly out-of-band.
    /// </summary>
    public interface IMarketplaceListingService
    {
        /// <summary>Public — paged search across active Marketplace listings.</summary>
        Task<Result<PagedResult<MarketplaceListingDto>>> SearchAsync(MarketplaceListingFilterRequestDto filter);

        /// <summary>
        /// Public — categories that currently have at least one Active
        /// listing, with per-bucket counts. Powers the Marketplace tab's
        /// chip row so chips never point at an empty bucket. Computed
        /// across the WHOLE active dataset (not paginated) so chip
        /// availability stays consistent regardless of page size.
        /// </summary>
        Task<Result<List<MarketplaceListingCategoryDto>>> GetCategoriesAsync();

        /// <summary>Public — single listing detail.</summary>
        Task<Result<MarketplaceListingDto>> GetByIdAsync(Guid id);

        /// <summary>Authenticated — listings owned by the calling user (any status).</summary>
        Task<Result<List<MarketplaceListingDto>>> GetMineAsync(Guid ownerUserId);

        /// <summary>Authenticated — creates a listing owned by the calling user.</summary>
        Task<Result<MarketplaceListingDto>> CreateAsync(Guid ownerUserId, CreateMarketplaceListingRequestDto request);

        /// <summary>Authenticated — owner-only status change (Sold / Archived / Active).</summary>
        Task<Result<MarketplaceListingDto>> UpdateStatusAsync(
            Guid ownerUserId,
            Guid listingId,
            UpdateMarketplaceListingStatusRequestDto request);

        /// <summary>
        /// Authenticated — owner-only partial update of listing fields.
        /// Only fields present (non-null) on the request are applied; all
        /// applied fields run the same length / range / enum validation
        /// rules as <see cref="CreateAsync"/>.
        /// </summary>
        Task<Result<MarketplaceListingDto>> UpdateAsync(
            Guid ownerUserId,
            Guid listingId,
            UpdateMarketplaceListingRequestDto request);

        /// <summary>Authenticated — owner-only image attachment.</summary>
        Task<Result<MarketplaceListingDto>> AddImageAsync(
            Guid ownerUserId,
            Guid listingId,
            AddMarketplaceListingImageRequestDto request);

        /// <summary>
        /// Authenticated — owner-only image removal. Verifies the image
        /// row actually belongs to the listing (not just that both ids
        /// exist) so a malicious client can't pass a foreign image id
        /// to remove someone else's photo.
        /// </summary>
        Task<Result<MarketplaceListingDto>> RemoveImageAsync(
            Guid ownerUserId,
            Guid listingId,
            Guid imageId);
    }
}
