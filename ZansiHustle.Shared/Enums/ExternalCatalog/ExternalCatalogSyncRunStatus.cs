namespace ZansiHustle.Shared.Enums.ExternalCatalog
{
    /// <summary>Lifecycle of a full-snapshot <c>ExternalCatalogSyncRun</c>. Archival (see the sync service) only ever happens from the Completed transition, never from InProgress or Failed.</summary>
    public enum ExternalCatalogSyncRunStatus
    {
        InProgress = 1,
        Completed = 2,
        Failed = 3,
    }
}
