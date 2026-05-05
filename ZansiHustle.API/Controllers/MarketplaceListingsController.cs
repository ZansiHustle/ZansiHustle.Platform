using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Marketplace;
using ZansiHustle.Application.Marketplace.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Casual peer-to-peer Marketplace endpoints — second-hand items
    /// posted by ordinary users.
    ///
    /// Reads (Search, GetById) are anonymous so guest browsing works
    /// before sign-in. Writes (Create, UpdateStatus, AddImage) require
    /// authentication AND ownership; ownership is enforced inside the
    /// service against the JWT-scoped user id.
    ///
    /// This controller intentionally has NO order / payment /
    /// merchant integration — Marketplace is a buyer-meets-seller
    /// channel and stays separate from the approved-merchant flow.
    /// </summary>
    [Route("api/marketplace-listings")]
    public class MarketplaceListingsController : BaseController
    {
        private readonly IMarketplaceListingService _service;
        private readonly ICurrentUserService _currentUserService;

        public MarketplaceListingsController(
            IMarketplaceListingService service,
            ICurrentUserService currentUserService)
        {
            _service = service;
            _currentUserService = currentUserService;
        }

        /// <summary>Public — paged search across active Marketplace listings.</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<PagedResult<MarketplaceListingDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Search([FromQuery] MarketplaceListingFilterRequestDto filter)
        {
            var result = await _service.SearchAsync(filter);
            return ToActionResult(result);
        }

        /// <summary>
        /// Public — category buckets across all currently-Active
        /// listings (with counts). Used by the Marketplace tab to
        /// render a chip row that's guaranteed to point at non-empty
        /// buckets.
        /// </summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<List<MarketplaceListingCategoryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            var result = await _service.GetCategoriesAsync();
            return ToActionResult(result);
        }

        /// <summary>Authenticated — listings owned by the calling user (any status).</summary>
        [HttpGet("mine")]
        [Authorize]
        [ProducesResponseType(typeof(Result<List<MarketplaceListingDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<List<MarketplaceListingDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.GetMineAsync(userId.Value);
            return ToActionResult(result);
        }

        /// <summary>Public — single listing detail.</summary>
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(Result<MarketplaceListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return ToActionResult(result);
        }

        /// <summary>Authenticated — creates a listing owned by the calling user.</summary>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(Result<MarketplaceListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateMarketplaceListingRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<MarketplaceListingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.CreateAsync(userId.Value, request);
            return ToActionResult(result);
        }

        /// <summary>Authenticated — owner-only status change (Sold / Archived / Active).</summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize]
        [ProducesResponseType(typeof(Result<MarketplaceListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateMarketplaceListingStatusRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<MarketplaceListingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.UpdateStatusAsync(userId.Value, id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Authenticated — owner-only partial update of listing details.
        /// Body fields are all optional; only present keys are applied.
        /// Image management lives on <c>POST /{id}/images</c> so this
        /// endpoint stays focused on text/details and never has to
        /// validate or re-upload binary content.
        /// </summary>
        [HttpPatch("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<MarketplaceListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateMarketplaceListingRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<MarketplaceListingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.UpdateAsync(userId.Value, id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Authenticated — owner-only image attachment.
        /// The binary upload itself is NOT performed here — the client
        /// uses the existing R2-backed media-upload pipeline
        /// (<see cref="ZansiHustle.Application.Media.IMediaService"/>)
        /// to upload the file and obtain a public URL, then posts that
        /// URL to this endpoint to attach it to the listing.
        /// </summary>
        [HttpPost("{id:guid}/images")]
        [Authorize]
        [ProducesResponseType(typeof(Result<MarketplaceListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddImage(
            Guid id,
            [FromBody] AddMarketplaceListingImageRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<MarketplaceListingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.AddImageAsync(userId.Value, id, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Authenticated — owner-only image removal. The route encodes
        /// both ids; the service double-checks that the image actually
        /// belongs to the listing so a foreign image id can't be used
        /// to delete someone else's photo via this route.
        /// </summary>
        [HttpDelete("{listingId:guid}/images/{imageId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<MarketplaceListingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveImage(Guid listingId, Guid imageId)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<MarketplaceListingDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _service.RemoveImageAsync(userId.Value, listingId, imageId);
            return ToActionResult(result);
        }
    }
}
