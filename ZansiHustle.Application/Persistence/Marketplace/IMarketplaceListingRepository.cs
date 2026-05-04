using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Marketplace.Dtos;
using ZansiHustle.Domain.Marketplace;

namespace ZansiHustle.Application.Persistence.Marketplace
{
    /// <summary>
    /// Persistence contract for <see cref="MarketplaceListing"/>.
    ///
    /// Mirrors the shape of <c>IListingRepository</c> for consistency
    /// without sharing any storage — Marketplace lives in its own table
    /// keyed off <c>OwnerUserId</c>, never <c>MerchantId</c>.
    /// </summary>
    public interface IMarketplaceListingRepository
    {
        Task<(List<MarketplaceListing> Items, int Total)> SearchAsync(MarketplaceListingFilterRequestDto filter);
        Task<MarketplaceListing?> GetByIdAsync(Guid id);
        Task<List<MarketplaceListing>> GetByOwnerAsync(Guid ownerUserId);

        Task AddAsync(MarketplaceListing listing);
        void Update(MarketplaceListing listing);
        void Delete(MarketplaceListing listing);

        Task AddImageAsync(MarketplaceListingImage image);
        Task<MarketplaceListingImage?> GetImageByIdAsync(Guid imageId);
        void RemoveImage(MarketplaceListingImage image);

        Task<bool> SaveChangesAsync();
    }
}
