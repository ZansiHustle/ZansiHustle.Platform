using System;

namespace ZansiHustle.Application.Finance.Dtos
{
    /// <summary>
    /// Outcome of one idempotent reconcile/backfill run. Safe to run repeatedly;
    /// counts let an admin verify what changed (new entries) vs. what was already
    /// booked (skipped) vs. excluded (cancelled/rejected) vs. failed.
    /// </summary>
    public sealed class LedgerReconcileResultDto
    {
        public Guid BatchId { get; set; }
        public int OrdersProcessed { get; set; }
        public int SellerEntriesCreated { get; set; }
        public int PlatformEntriesCreated { get; set; }
        public int SkippedExcluded { get; set; }
        public int SkippedAlreadyPresent { get; set; }
        public int Failures { get; set; }
    }
}
