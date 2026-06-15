using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.CustomerFinance;
using ZansiHustle.Application.CustomerFinance.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Customer-facing financial overview (spending + transactions). All routes
    /// scope to the authenticated user via the JWT — a customer can only ever see
    /// their own finance data. Never exposes seller earnings/payout or provider
    /// internals.
    /// </summary>
    [Route("api/me/finance")]
    [Authorize]
    public class CustomerFinanceController : BaseController
    {
        private readonly ICustomerFinanceService _finance;
        private readonly ICurrentUserService _currentUser;

        public CustomerFinanceController(ICustomerFinanceService finance, ICurrentUserService currentUser)
        {
            _finance = finance;
            _currentUser = currentUser;
        }

        /// <summary>Spending summary (week / month / all-time / refunds + breakdowns).</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(Result<FinanceSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<FinanceSummaryDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _finance.GetSummaryAsync(userId.Value));
        }

        /// <summary>
        /// Combined transaction list (orders, bookings, refunds, wallet moves).
        /// <paramref name="type"/>: all|orders|bookings|wallet|refunds.
        /// <paramref name="range"/>: 7d|30d|month|year|all.
        /// </summary>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(Result<FinanceTransactionsPageDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] string? type = "all",
            [FromQuery] string? range = "30d",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30)
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<FinanceTransactionsPageDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _finance.GetTransactionsAsync(userId.Value, type, range, page, pageSize));
        }
    }
}
