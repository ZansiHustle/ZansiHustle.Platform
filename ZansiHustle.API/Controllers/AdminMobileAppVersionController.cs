using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.AppVersion;
using ZansiHustle.Application.AppVersion.Dtos;
using ZansiHustle.Application.Common.Interfaces.Shared;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin management of mobile version-control rules (Portal Super Admin grid).
    /// One rule per (Platform, Channel); rules are activated/deactivated rather
    /// than hard-deleted. Same role set as <see cref="AdminOrdersController"/>.
    /// </summary>
    [Route("api/admin/mobile-app-version")]
    [Authorize(Roles = "SuperAdmin,Admin,Partner,Accountant")]
    public class AdminMobileAppVersionController : BaseController
    {
        private readonly IMobileAppVersionService _service;
        private readonly ICurrentUserService _currentUserService;

        public AdminMobileAppVersionController(
            IMobileAppVersionService service,
            ICurrentUserService currentUserService)
        {
            _service = service;
            _currentUserService = currentUserService;
        }

        /// <summary>All rules (enabled + disabled), ordered by platform/channel.</summary>
        [HttpGet("rules")]
        public async Task<IActionResult> GetRules(CancellationToken cancellationToken)
        {
            var result = await _service.GetRulesAsync(cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>Single rule by id.</summary>
        [HttpGet("rules/{id:guid}")]
        public async Task<IActionResult> GetRule(Guid id, CancellationToken cancellationToken)
        {
            var result = await _service.GetRuleAsync(id, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>Create a new rule. Rejects a duplicate (Platform, Channel).</summary>
        [HttpPost("rules")]
        public async Task<IActionResult> CreateRule(
            [FromBody] UpsertMobileAppVersionRuleRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _service.CreateRuleAsync(request, _currentUserService.UserId, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>Update an existing rule.</summary>
        [HttpPut("rules/{id:guid}")]
        public async Task<IActionResult> UpdateRule(
            Guid id,
            [FromBody] UpsertMobileAppVersionRuleRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _service.UpdateRuleAsync(id, request, _currentUserService.UserId, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>Activate (enable) a rule.</summary>
        [HttpPost("rules/{id:guid}/activate")]
        public async Task<IActionResult> ActivateRule(Guid id, CancellationToken cancellationToken)
        {
            var result = await _service.SetRuleEnabledAsync(id, true, _currentUserService.UserId, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>Deactivate (disable) a rule. No hard delete.</summary>
        [HttpPost("rules/{id:guid}/deactivate")]
        public async Task<IActionResult> DeactivateRule(Guid id, CancellationToken cancellationToken)
        {
            var result = await _service.SetRuleEnabledAsync(id, false, _currentUserService.UserId, cancellationToken);
            return ToActionResult(result);
        }
    }
}
