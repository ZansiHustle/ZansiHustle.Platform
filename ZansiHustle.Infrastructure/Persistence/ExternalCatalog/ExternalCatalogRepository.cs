using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Persistence.ExternalCatalog;
using ZansiHustle.Domain.ExternalCatalog;
using ZansiHustle.Infrastructure.Data;

namespace ZansiHustle.Infrastructure.Persistence.ExternalCatalog
{
    public class ExternalCatalogRepository : IExternalCatalogRepository
    {
        private readonly AppDbContext _context;

        public ExternalCatalogRepository(AppDbContext context)
        {
            _context = context;
        }

        // Plain tracked reads (same convention as PaymentRepository): within
        // one Scoped DbContext's lifetime, a second read of the same row
        // returns the SAME tracked CLR instance via EF's identity map, so
        // mutating it directly and calling Update() (a harmless re-attach on
        // an already-tracked instance) is safe. AsNoTracking() here would
        // create a SECOND, conflicting instance for a key the context is
        // already tracking from an earlier call in the same unit of work.

        public async Task<ExternalCatalogSyncRun?> GetSyncRunByIdAsync(Guid id)
        {
            return await _context.ExternalCatalogSyncRuns.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddSyncRunAsync(ExternalCatalogSyncRun run)
        {
            ArgumentNullException.ThrowIfNull(run);
            await _context.ExternalCatalogSyncRuns.AddAsync(run);
        }

        public void UpdateSyncRun(ExternalCatalogSyncRun run)
        {
            ArgumentNullException.ThrowIfNull(run);
            _context.ExternalCatalogSyncRuns.Update(run);
        }

        public async Task<ExternalCatalogSourceStatus?> GetSourceStatusAsync(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode)) return null;
            var normalised = sourceCode.Trim().ToLowerInvariant();
            return await _context.ExternalCatalogSourceStatuses.FirstOrDefaultAsync(x => x.SourceCode == normalised);
        }

        public async Task AddSourceStatusAsync(ExternalCatalogSourceStatus status)
        {
            ArgumentNullException.ThrowIfNull(status);
            await _context.ExternalCatalogSourceStatuses.AddAsync(status);
        }

        public void UpdateSourceStatus(ExternalCatalogSourceStatus status)
        {
            ArgumentNullException.ThrowIfNull(status);
            _context.ExternalCatalogSourceStatuses.Update(status);
        }

        public async Task<ExternalCategoryMapping?> GetCategoryMappingAsync(string sourceCode, string externalCategoryCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode) || string.IsNullOrWhiteSpace(externalCategoryCode)) return null;
            var source = sourceCode.Trim().ToLowerInvariant();
            var code = externalCategoryCode.Trim();
            return await _context.ExternalCategoryMappings
                .FirstOrDefaultAsync(x => x.SourceCode == source && x.ExternalCategoryCode == code);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
