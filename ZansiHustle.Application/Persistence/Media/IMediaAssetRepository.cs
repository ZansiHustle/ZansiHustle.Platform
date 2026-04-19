using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Media;
using ZansiHustle.Shared.Enums.Media;

namespace ZansiHustle.Application.Persistence.Media
{
    public interface IMediaAssetRepository
    {
        Task<MediaAsset?> GetByIdAsync(Guid id);
        Task<List<MediaAsset>> GetByOwnerAsync(OwnerEntityType ownerType, Guid ownerId);
        Task<List<MediaAsset>> GetByIdsAsync(IEnumerable<Guid> ids);
        /// <summary>Used to backfill orphan uploads onto a freshly-created owner row.</summary>
        Task<List<MediaAsset>> GetOrphansForUserAsync(Guid uploadedByUserId, IEnumerable<Guid> ids);
        Task AddAsync(MediaAsset entity);
        void Update(MediaAsset entity);
        Task<bool> SaveChangesAsync();
    }
}
