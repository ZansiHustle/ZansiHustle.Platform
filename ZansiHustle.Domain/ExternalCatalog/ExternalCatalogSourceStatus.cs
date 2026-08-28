using System;

namespace ZansiHustle.Domain.ExternalCatalog
{
    /// <summary>
    /// One row per registered external catalog source — the observability
    /// surface for "last sync time / last count / last failure / enabled"
    /// (section 22 of the sync spec) without needing to aggregate the
    /// potentially-large <see cref="ExternalCatalogSyncRun"/> history on
    /// every status check. Upserted on every sync-relevant operation
    /// (incremental upsert, archive, full-sync batch/complete/fail).
    /// </summary>
    public class ExternalCatalogSourceStatus
    {
        public Guid Id { get; set; }

        /// <summary>Unique. The registered source's lowercase code, e.g. "zansitech".</summary>
        public string SourceCode { get; set; } = string.Empty;

        public DateTime? LastActivityAtUtc { get; set; }

        /// <summary>Free-form label: "Upsert" | "Archive" | "FullSyncBatch" | "FullSyncComplete" | "FullSyncFailed".</summary>
        public string? LastActivityType { get; set; }

        public bool LastActivitySucceeded { get; set; }
        public string? LastError { get; set; }

        public int TotalProductsUpserted { get; set; }
        public int TotalProductsArchived { get; set; }

        public Guid? LastFullSyncRunId { get; set; }
        public DateTime? LastFullSyncCompletedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
