using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Merchants.Dtos;
using ZansiHustle.Domain.Merchants;

namespace ZansiHustle.Application.Persistence.Merchants
{
    /// <summary>
    /// Repository contract for merchant persistence operations.
    /// </summary>
    public interface IMerchantRepository
    {
        Task<List<Merchant>> GetAllAsync();
        Task<List<Merchant>> GetByOwnerAsync(Guid ownerUserId);
        Task<Merchant?> GetByIdAsync(Guid id);
        Task<Merchant?> GetByCodeAsync(string code);
        Task<Merchant?> GetBySlugAsync(string slug);
        Task<bool> ExistsByCodeAsync(string code);
        Task<bool> ExistsBySlugAsync(string slug);
        Task AddAsync(Merchant merchant);
        void Update(Merchant merchant);
        void Delete(Merchant merchant);
        Task<bool> SaveChangesAsync();

        /// <summary>
        /// Public discovery search. Returns ONLY <c>Status == Active</c>
        /// merchants — Pending, Inactive and Suspended are filtered out
        /// at the repository level so a controller bug can't accidentally
        /// leak them through the public route.
        ///
        /// Filtering, sorting and pagination semantics:
        ///   • Text/category/province/city filters run in SQL.
        ///   • When <paramref name="filter"/> supplies <c>Lat</c> +
        ///     <c>Lng</c>, a coarse latitude/longitude bbox is applied
        ///     in SQL using a <c>RadiusKm</c> defaulted to 25 km, then
        ///     exact Haversine distance is computed in memory and the
        ///     result is filtered to that radius and sorted ascending
        ///     by distance.
        ///   • Without coords: SQL pagination, sorted by <c>Sort</c>
        ///     (<c>rating</c> default, <c>newest</c> alternative).
        /// </summary>
        Task<(List<Merchant> Items, int Total)> SearchPublicAsync(
            MerchantPublicFilterRequestDto filter);

        /// <summary>
        /// Public discovery detail — returns null when the merchant
        /// does not exist OR <c>Status != Active</c>. The controller
        /// maps both to a 404 so the existence of a Pending/Suspended
        /// merchant is never disclosed publicly.
        /// </summary>
        Task<Merchant?> GetPublicByIdAsync(Guid id);
    }
}
