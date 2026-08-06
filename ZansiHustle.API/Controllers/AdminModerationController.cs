using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Admin.Moderation;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin moderation enforcement (App Store Guideline 1.2): suspend/ban users
    /// and take down / restore content. Distinct from the merchant approval
    /// workflow — this acts on app USERS and individual content items.
    /// </summary>
    [Route("api/admin/moderation")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Support,Moderator")]
    public class AdminModerationController : BaseController
    {
        private readonly IAdminModerationService _moderation;
        private readonly ICurrentUserService _currentUserService;

        public AdminModerationController(IAdminModerationService moderation, ICurrentUserService currentUserService)
        {
            _moderation = moderation;
            _currentUserService = currentUserService;
        }

        private Guid? Moderator => _currentUserService.UserId;
        private IActionResult Unauth() =>
            ToActionResult(Result.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

        // ── Users ──
        [HttpPost("users/{id:guid}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid id, [FromBody] ModerationActionDto? body)
            => Moderator is null ? Unauth() : ToActionResult(await _moderation.SuspendUserAsync(id, Moderator.Value, body?.Reason));

        [HttpPost("users/{id:guid}/ban")]
        public async Task<IActionResult> BanUser(Guid id, [FromBody] ModerationActionDto? body)
            => Moderator is null ? Unauth() : ToActionResult(await _moderation.BanUserAsync(id, Moderator.Value, body?.Reason));

        [HttpPost("users/{id:guid}/reinstate")]
        public async Task<IActionResult> ReinstateUser(Guid id)
            => Moderator is null ? Unauth() : ToActionResult(await _moderation.ReinstateUserAsync(id, Moderator.Value));

        // ── Listings ──
        [HttpPost("listings/{id:guid}/hide")]
        public async Task<IActionResult> HideListing(Guid id, [FromBody] ModerationActionDto? body)
            => Moderator is null ? Unauth() : ToActionResult(await _moderation.HideListingAsync(id, Moderator.Value, body?.Reason));

        [HttpPost("listings/{id:guid}/unhide")]
        public async Task<IActionResult> UnhideListing(Guid id)
            => Moderator is null ? Unauth() : ToActionResult(await _moderation.UnhideListingAsync(id, Moderator.Value));

        // ── Reviews ──
        [HttpPost("reviews/{id:guid}/hide")]
        public async Task<IActionResult> HideReview(Guid id, [FromBody] ModerationActionDto? body)
            => Moderator is null ? Unauth() : ToActionResult(await _moderation.HideReviewAsync(id, Moderator.Value, body?.Reason));

        [HttpPost("reviews/{id:guid}/unhide")]
        public async Task<IActionResult> UnhideReview(Guid id)
            => Moderator is null ? Unauth() : ToActionResult(await _moderation.UnhideReviewAsync(id, Moderator.Value));
    }
}
