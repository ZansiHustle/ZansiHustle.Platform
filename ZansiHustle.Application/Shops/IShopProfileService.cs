using System;
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
        Task<Result<ShopProfileDto>> CreateMineAsync(Guid ownerUserId, CreateShopRequestDto request);
        Task<Result<ShopProfileDto>> UpdateMineAsync(Guid ownerUserId, Guid shopId, UpdateShopRequestDto request);

        Task<Result<ShopProfilePublicDto>> GetPublicByIdAsync(Guid id);
        Task<Result<PagedResult<ShopProfilePublicDto>>> SearchPublicAsync(int page, int pageSize, string? q);

        Task<Result<PagedResult<ShopProfileDto>>> SearchAdminAsync(int page, int pageSize, ShopProfileStatus? status, string? q);

        Task<Result<ShopProfileDto>> SuspendAsync(Guid id, string? reason);
        Task<Result<ShopProfileDto>> ReactivateAsync(Guid id);
    }
}
