using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Finance.Dtos;

namespace ZansiHustle.Application.Finance
{
    /// <summary>
    /// Idempotent reconcile/backfill of the seller + platform finance ledgers
    /// from PAID orders. Safe to run repeatedly: it skips orders already booked
    /// (guarded by a filtered unique index) and also catches NEW eligible orders,
    /// so it can be re-run as a recurring reconcile job. Purely additive — never
    /// touches payments, order lifecycle, dispatch, wallet, or withdrawals.
    /// </summary>
    public interface ILedgerReconcileService
    {
        /// <param name="batchId">Optional batch id; generated when null.</param>
        Task<LedgerReconcileResultDto> ReconcileAsync(Guid? batchId = null);

        /// <summary>Platform-wide roll-up read from the ledgers.</summary>
        Task<PlatformFinanceSummaryDto> GetPlatformSummaryAsync();
    }
}
