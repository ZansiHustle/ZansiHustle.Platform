using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Finance;
using ZansiHustle.Application.Finance.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin finance — writes/reads the seller + platform accounting books.
    /// Gated to SuperAdmin only (stricter than analytics) because reconcile
    /// MUTATES the ledgers. Purely additive accounting: it never touches
    /// payments, order lifecycle, dispatch, wallet, or withdrawals.
    /// </summary>
    [Route("api/admin/finance")]
    [Authorize(Roles = "SuperAdmin")]
    public class AdminFinanceController : BaseController
    {
        private readonly ILedgerReconcileService _reconcile;

        public AdminFinanceController(ILedgerReconcileService reconcile)
        {
            _reconcile = reconcile;
        }

        /// <summary>
        /// Idempotently backfills/reconciles the ledgers from PAID orders. Safe
        /// to run repeatedly; also picks up newly-eligible orders. Returns counts.
        /// </summary>
        [HttpPost("reconcile-ledger")]
        [ProducesResponseType(typeof(Result<LedgerReconcileResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReconcileLedger()
        {
            var result = await _reconcile.ReconcileAsync();
            return ToActionResult(Result<LedgerReconcileResultDto>.Success(
                result, "Ledger reconcile completed."));
        }

        /// <summary>
        /// Platform-wide finance roll-up read from the ledgers. Seller funds and
        /// platform funds are kept separate; gateway fee is a cost, not profit.
        /// </summary>
        [HttpGet("platform-summary")]
        [ProducesResponseType(typeof(Result<PlatformFinanceSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPlatformSummary()
        {
            var summary = await _reconcile.GetPlatformSummaryAsync();
            return ToActionResult(Result<PlatformFinanceSummaryDto>.Success(
                summary, "Platform finance summary computed."));
        }
    }
}
