using System;
using ZansiHustle.Shared.Enums.Finance;

namespace ZansiHustle.Domain.Finance
{
    /// <summary>
    /// Append-only platform book. One row per (order, entry type): platform-fee
    /// revenue (5%), gateway-fee cost (3%, NOT profit), and delivery pass-through.
    /// Kept strictly separate from seller funds (see <see cref="SellerLedgerEntry"/>).
    /// Money column is decimal(18,2).
    /// </summary>
    public class PlatformLedgerEntry
    {
        public Guid Id { get; set; }

        public Guid? OrderId { get; set; }
        public Guid? ServiceBookingId { get; set; }

        public LedgerSourceType SourceType { get; set; }
        public PlatformLedgerEntryType EntryType { get; set; }

        /// <summary>Positive magnitude of this entry (revenue/cost/pass-through).</summary>
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "ZAR";

        /// <summary>When the underlying economic event occurred.</summary>
        public DateTime OccurredAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }

        /// <summary>True when produced by the reconcile/backfill job.</summary>
        public bool IsBackfilled { get; set; }

        /// <summary>Groups all rows produced by a single reconcile run.</summary>
        public Guid? BackfillBatchId { get; set; }
    }
}
