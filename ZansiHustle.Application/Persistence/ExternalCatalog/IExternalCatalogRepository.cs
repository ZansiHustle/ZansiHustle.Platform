using System;
using System.Threading.Tasks;
using ZansiHustle.Domain.ExternalCatalog;

namespace ZansiHustle.Application.Persistence.ExternalCatalog
{
    /// <summary>
    /// Persistence for the three small external-catalog-sync tracking
    /// entities. Deliberately separate from <c>IListingRepository</c> —
    /// that repository owns Listing/ListingVariant persistence; this one
    /// owns sync observability + category mapping only.
    /// </summary>
    public interface IExternalCatalogRepository
    {
        Task<ExternalCatalogSyncRun?> GetSyncRunByIdAsync(Guid id);
        Task AddSyncRunAsync(ExternalCatalogSyncRun run);
        void UpdateSyncRun(ExternalCatalogSyncRun run);

        Task<ExternalCatalogSourceStatus?> GetSourceStatusAsync(string sourceCode);
        Task AddSourceStatusAsync(ExternalCatalogSourceStatus status);
        void UpdateSourceStatus(ExternalCatalogSourceStatus status);

        Task<ExternalCategoryMapping?> GetCategoryMappingAsync(string sourceCode, string externalCategoryCode);

        Task<bool> SaveChangesAsync();
    }
}
