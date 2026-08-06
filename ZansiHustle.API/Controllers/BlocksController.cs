using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Blocks;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// User-to-user blocking (App Store Guideline 1.2). A block prevents chat in
    /// both directions and lets the app hide the blocked user's content.
    /// </summary>
    [Route("api/blocks")]
    [Authorize]
    public class BlocksController : BaseController
    {
        private readonly IUserBlockService _blockService;
        private readonly ICurrentUserService _currentUserService;

        public BlocksController(IUserBlockService blockService, ICurrentUserService currentUserService)
        {
            _blockService = blockService;
            _currentUserService = currentUserService;
        }

        /// <summary>The users the caller has blocked.</summary>
        [HttpGet]
        public async Task<IActionResult> MyBlocks()
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _blockService.GetMyBlocksAsync(userId.Value));
        }

        /// <summary>Block a user.</summary>
        [HttpPost]
        public async Task<IActionResult> Block([FromBody] BlockUserRequestDto request)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _blockService.BlockAsync(userId.Value, request));
        }

        /// <summary>Unblock a previously-blocked user.</summary>
        [HttpDelete("{blockedUserId:guid}")]
        public async Task<IActionResult> Unblock(Guid blockedUserId)
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _blockService.UnblockAsync(userId.Value, blockedUserId));
        }
    }
}
