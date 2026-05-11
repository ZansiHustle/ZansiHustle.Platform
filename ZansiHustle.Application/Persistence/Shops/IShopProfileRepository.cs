using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Shared.Enums.Shops;

namespace ZansiHustle.Application.Persistence.Shops
{
    /// <summary>
    /// Persistence contract for <see cref="ShopProfile"/>. Service
    /// layer never reaches into <c>AppDbContext</c> directly — every
    /// query that joins across DbSets (Merchant ownership checks,
    /// reviewer joins, etc.) lives here.
    /// </summary>
    public interface IShopProfileRepository
    {
        Task<ShopProfile?> GetByIdAsync(Guid id);
        Task<ShopProfile?> GetBySlugAsync(string slug);

        /// <summary>
        /// Returns the caller's non-Suspended ShopProfile (if any).
        /// Used by GET /api/shops/mine and the duplicate-create guard.
        /// Filters by merchant ownership (via Merchant.OwnerUserId).
        /// </summary>
        Task<ShopProfile?> GetMineAsync(Guid ownerUserId);

        /// <summary>
        /// Returns any non-Suspended ShopProfile for the given merchant.
        /// Used by the duplicate-create guard at the service layer.
        /// </summary>
        Task<ShopProfile?> GetActiveByMerchantAsync(Guid merchantId);

        /// <summary>True if a different shop with the slug already exists.</summary>
        Task<bool> SlugExistsAsync(string slug, Guid? excludingId = null);

        /// <summary>
        /// Public listing — only <see cref="ShopProfileStatus.Active"/>
        /// rows. Pagination capped at 100 in the service layer.
        /// </summary>
        Task<PagedResult<ShopProfile>> SearchPublicAsync(int page, int pageSize, string? q);

        /// <summary>
        /// Admin listing — all statuses. Optional status filter.
        /// Powers Portal /shops.
        /// </summary>
        Task<PagedResult<ShopProfile>> SearchAdminAsync(int page, int pageSize, ShopProfileStatus? status, string? q);

        Task AddAsync(ShopProfile shop);
        void Update(ShopProfile shop);
        Task<bool> SaveChangesAsync();
    }
}
