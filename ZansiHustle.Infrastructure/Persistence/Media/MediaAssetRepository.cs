using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.Media;
using ZansiHustle.Domain.Media;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Media;

namespace ZansiHustle.Infrastructure.Persistence.Media
{
    public class MediaAssetRepository : IMediaAssetRepository
    {
        private readonly AppDbContext _context;

        public MediaAssetRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<MediaAsset?> GetByIdAsync(Guid id)
            => _context.MediaAssets.FirstOrDefaultAsync(x => x.Id == id);

        public Task<List<MediaAsset>> GetByOwnerAsync(OwnerEntityType ownerType, Guid ownerId)
            => _context.MediaAssets
                .Where(x => x.OwnerEntityType == ownerType && x.OwnerEntityId == ownerId)
                .OrderBy(x => x.Purpose).ThenBy(x => x.SortOrder).ThenBy(x => x.CreatedAtUtc)
                .ToListAsync();

        public Task<List<MediaAsset>> GetByIdsAsync(IEnumerable<Guid> ids)
        {
            var set = ids.Distinct().ToArray();
            if (set.Length == 0) return Task.FromResult(new List<MediaAsset>());
            return _context.MediaAssets.Where(x => set.Contains(x.Id)).ToListAsync();
        }

        public Task<List<MediaAsset>> GetOrphansForUserAsync(Guid uploadedByUserId, IEnumerable<Guid> ids)
        {
            var set = ids.Distinct().ToArray();
            if (set.Length == 0) return Task.FromResult(new List<MediaAsset>());
            return _context.MediaAssets
                .Where(x => set.Contains(x.Id)
                            && x.UploadedByUserId == uploadedByUserId
                            && x.OwnerEntityId == null)
                .ToListAsync();
        }

        public async Task AddAsync(MediaAsset entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await _context.MediaAssets.AddAsync(entity);
        }

        public void Update(MediaAsset entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.MediaAssets.Update(entity);
        }

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
    }
}
