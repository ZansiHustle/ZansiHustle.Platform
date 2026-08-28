using System;
using ZansiHustle.Shared.Enums.ExternalCatalog;

namespace ZansiHustle.Domain.ExternalCatalog
{
    /// <summary>
    /// One full-snapshot sync batch from an external catalog source (e.g.
    /// ZansiTech "sync all products"). Exists purely to give full-sync an
    /// explicit completion boundary: Listings touched by this run are
    /// tagged with its Id (see <c>Listing.ExternalSyncRunId</c>), and only
    /// on a successful <c>/complete</c> call are untouched Listings for
    /// this source archived. A run that is never completed (abandoned,
    /// failed) never triggers archival — this is what stops one dropped
    /// connection or partial batch from being read as "delete everything
    /// missing".
    /// </summary>
    public class ExternalCatalogSyncRun
    {
        public Guid Id { get; set; }
        public string SourceCode { get; set; } = string.Empty;
        public ExternalCatalogSyncRunStatus Status { get; set; } = ExternalCatalogSyncRunStatus.InProgress;

        public int ProductsUpserted { get; set; }
        public int VariantsUpserted { get; set; }
        public int ProductsArchived { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }
    }
}
