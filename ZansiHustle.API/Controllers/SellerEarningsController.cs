using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Seller.Earnings;
using ZansiHustle.Application.Seller.Earnings.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Seller proceeds / earnings. SELLER money — separate from the customer
    /// refund/spend wallet. The caller only ever sees their own merchants' data.
    /// </summary>
    [Route("api/seller/earnings")]
    [Authorize]
    public class SellerEarningsController : BaseController
    {
        private readonly ISellerEarningsService _earnings;
        private readonly ICurrentUserService _currentUserService;

        public SellerEarningsController(
            ISellerEarningsService earnings,
            ICurrentUserService currentUserService)
        {
            _earnings = earnings;
            _currentUserService = currentUserService;
        }

        /// <summary>Earnings summary over a range ("7d" | "30d" | "month" | "90d" | "all").</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(Result<SellerEarningsSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary([FromQuery] string? range)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<SellerEarningsSummaryDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _earnings.GetSummaryAsync(userId.Value, range));
        }

        /// <summary>
        /// Clean ALL-TIME finance summary for the seller dashboard card:
        /// total earned / paid out / pending payout. Server-authoritative.
        /// </summary>
        [HttpGet("finance-summary")]
        [ProducesResponseType(typeof(Result<SellerFinanceSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFinanceSummary()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<SellerFinanceSummaryDto>.Failure(
                    ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _earnings.GetFinanceSummaryAsync(userId.Value));
        }
    }
}
