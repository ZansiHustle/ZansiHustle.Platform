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

        /// <summary>Authenticated — owner-only image attachment.</summary>
        Task<Result<MarketplaceListingDto>> AddImageAsync(
            Guid ownerUserId,
            Guid listingId,
            AddMarketplaceListingImageRequestDto request);
    }
}
