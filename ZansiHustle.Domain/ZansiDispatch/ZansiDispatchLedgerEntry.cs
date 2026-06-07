using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Domain.ZansiDispatch
{
    /// <summary>
    /// Append-only audit ledger for delivery-fee movements and reconciliation
    /// (quote charged, actual courier cost, surplus/deficit recognised, manual
    /// adjustments, refunds). One shipment produces several entries over its
    /// life. All timestamps are UTC.
    /// </summary>
    public class ZansiDispatchLedgerEntry
    {
        public Guid Id { get; set; }

        public Guid? ShipmentId { get; set; }
        public Guid? OrderId { get; set; }

        public ZansiDispatchLedgerEntryType EntryType { get; set; }

        public decimal Amount { get; set; }
        public ZansiDispatchLedgerDirection Direction { get; set; }

        /// <summary>Signed impact on the logistics balance (credit positive, debit negative).</summary>
        public decimal BalanceImpact { get; set; }

        public string? Description { get; set; }
        public string? Reference { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
