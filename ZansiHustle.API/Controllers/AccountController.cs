using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Account;
using ZansiHustle.Application.Account.Dtos;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Self-service account management for the signed-in user.
    /// Currently exposes permanent account deletion (App Store 5.1.1(v)).
    /// </summary>
    [Route("api/account")]
    [Authorize]
    public class AccountController : BaseController
    {
        private readonly IAccountDeletionService _deletionService;
        private readonly ICurrentUserService _currentUserService;

        public AccountController(
            IAccountDeletionService deletionService,
            ICurrentUserService currentUserService)
        {
            _deletionService = deletionService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Permanently deletes the caller's own account: anonymises all personal
        /// information, removes their public content from discovery, revokes every
        /// session, and blocks any future sign-in. This cannot be undone.
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteMyAccount([FromBody] DeleteAccountRequestDto? request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _deletionService.DeleteMyAccountAsync(userId.Value, request?.Reason);
            return ToActionResult(result);
        }
    }
}
