using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Domain.Listings;

namespace ZansiHustle.Application.Persistence.Listings
{
    /// <summary>
    /// Persistence contract for <see cref="Listing"/>.
    /// </summary>
    public interface IListingRepository
    {
        Task<(List<Listing> Items, int Total)> SearchAsync(ListingFilterRequestDto filter);
        Task<Listing?> GetByIdAsync(Guid id);
        Task<Listing?> GetBySlugAsync(string slug);
        Task<List<Listing>> GetByMerchantAsync(Guid merchantId);
        Task<List<Listing>> GetByOwnerAsync(Guid ownerUserId);
        Task<bool> ExistsBySlugAsync(string slug);
        Task AddAsync(Listing listing);
        void Update(Listing listing);
        void Delete(Listing listing);
        Task<bool> SaveChangesAsync();
    }
}
