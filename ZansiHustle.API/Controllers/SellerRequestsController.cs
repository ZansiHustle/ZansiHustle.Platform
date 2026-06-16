using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Sellers.Requests;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Seller "actionable requests" — drives the Sell-tab badge. Scoped to the
    /// authenticated seller via the JWT; a non-seller safely gets zeros.
    /// </summary>
    [Route("api/seller/requests")]
    [Authorize]
    public class SellerRequestsController : BaseController
    {
        private readonly ISellerRequestsService _requests;
        private readonly ICurrentUserService _currentUser;

        public SellerRequestsController(ISellerRequestsService requests, ICurrentUserService currentUser)
        {
            _requests = requests;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Count of the seller's actionable pending requests (product orders
        /// awaiting acceptance + service bookings awaiting provider acceptance).
        /// </summary>
        [HttpGet("pending-count")]
        [ProducesResponseType(typeof(Result<SellerRequestCountDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingCount()
        {
            var userId = _currentUser.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result<SellerRequestCountDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            return ToActionResult(await _requests.GetPendingCountAsync(userId.Value));
        }
    }
}
