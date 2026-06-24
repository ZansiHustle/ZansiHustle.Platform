using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Shops.Dtos;
using ZansiHustle.Shared.Enums.Shops;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Shops
{
    /// <summary>
    /// Service contract for shop-storefront operations. Distinct from
    /// <c>IMerchantService</c> — merchants are the seller's business
    /// account; shop profiles are the optional storefront the seller
    /// opens on top.
    /// </summary>
    public interface IShopProfileService
    {
        Task<Result<ShopProfileDto?>> GetMineAsync(Guid ownerUserId);

        /// <summary>
        /// All of the caller's shops (owner-facing My Shops list). Normal
        /// sellers see 0 or 1; Admin/SuperAdmin owners may see several. Returns
        /// an empty list (never null) when the seller has no shop yet.
        /// </summary>
        Task<Result<List<ShopProfileDto>>> GetAllMineAsync(Guid ownerUserId);

        Task<Result<ShopProfileDto>> CreateMineAsync(Guid ownerUserId, CreateShopRequestDto request);
        Task<Result<ShopProfileDto>> UpdateMineAsync(Guid ownerUserId, Guid shopId, UpdateShopRequestDto request);

        /// <summary>
        /// Seller pauses / resumes their shop's buyer-facing visibility. Pausing
        /// hides the shop + its attached listings from public discovery without
        /// deleting anything or changing listing statuses. Self-resume is blocked
        /// when the shop is admin-held (UnderReview / Blocked).
        /// </summary>
        Task<Result<ShopProfileDto>> UpdateVisibilityAsync(Guid ownerUserId, Guid shopId, bool isPaused, string? reason);

        /// <summary>
        /// Public shop detail. <paramref name="viewerUserId"/> is the
        /// authenticated caller (null for anonymous). A paused/hidden shop 404s
        /// for buyers, but the OWNER can still load it so their preview works
        /// and can surface the "paused" banner.
        /// </summary>
        Task<Result<ShopProfilePublicDto>> GetPublicByIdAsync(Guid id, Guid? viewerUserId = null);
        Task<Result<PagedResult<ShopProfilePublicDto>>> SearchPublicAsync(int page, int pageSize, string? q);

        Task<Result<PagedResult<ShopProfileDto>>> SearchAdminAsync(int page, int pageSize, ShopProfileStatus? status, string? q);

        Task<Result<ShopProfileDto>> SuspendAsync(Guid id, string? reason);
        Task<Result<ShopProfileDto>> ReactivateAsync(Guid id);
    }
}
