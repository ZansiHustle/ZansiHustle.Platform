using System;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Sellers.Incidents
{
    /// <summary>
    /// Seller accountability foundation. Records fulfilment incidents against a
    /// merchant and (admin-driven) applies/waives the associated penalty. V1 is
    /// conservative: <see cref="RecordAsync"/> creates a PendingReview incident
    /// with NO automatic payout impact — an admin explicitly applies. The customer
    /// is never affected by this; customer refunds happen separately on the order.
    /// </summary>
    public interface ISellerIncidentService
    {
        /// <summary>Records a new incident (PendingReview). Best-effort, never throws to the caller.</summary>
        Task<Result<Guid>> RecordAsync(RecordSellerIncidentInput input, CancellationToken ct = default);

        /// <summary>Admin applies the penalty — reduces the seller's future payout.</summary>
        Task<Result> ApplyAsync(Guid incidentId, string? adminNotes, CancellationToken ct = default);

        /// <summary>Admin waives (dismisses) the incident — no payout impact.</summary>
        Task<Result> WaiveAsync(Guid incidentId, string? adminNotes, CancellationToken ct = default);

        /// <summary>
        /// Total of APPLIED penalty amounts for a merchant. The seller earnings/
        /// payout layer subtracts this — the payout may legitimately go negative.
        /// </summary>
        Task<decimal> GetAppliedPenaltyTotalAsync(Guid merchantId, CancellationToken ct = default);
    }

    /// <summary>Input for recording a seller incident.</summary>
    public sealed class RecordSellerIncidentInput
    {
        public Guid MerchantId { get; set; }
        public Guid? OrderId { get; set; }
        public SellerIncidentType IncidentType { get; set; }
        public SellerIncidentSeverity Severity { get; set; } = SellerIncidentSeverity.Medium;
        /// <summary>Penalty amount earmarked (0 to record without a proposed charge).</summary>
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string Reason { get; set; } = string.Empty;
    }
}
