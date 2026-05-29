using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Agents.AgentPayouts;
using ZansiHustle.Application.Agents.AgentPayouts.Dtos;
using ZansiHustle.Application.Agents.AgentProvisioning;
using ZansiHustle.Application.Agents.AgentProvisioning.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin provisioning + earnings/payout management for agents.
    ///
    /// Provisioning (Create / Get / Suspend / Reset password) is open
    /// to Admin / SuperAdmin / Partner / MarketplaceGrowthAssociate /
    /// TeamManager — the operational team that owns the agent pipeline.
    ///
    /// Payout endpoints split by audience:
    ///   • `/api/agents/{id}/payouts`   + `/earnings-summary` — admin
    ///     read+write. Same role gate as provisioning.
    ///   • `/api/agents/me/payouts`     + `/earnings-summary` — agent
    ///     self-view. Role: Agent only. Scoped to the caller's own id
    ///     (read from the JWT `sub`/`nameidentifier` claim) so an agent
    ///     can never view someone else's history by guessing a route.
    ///
    /// Why the agent endpoints are NOT a sub-resource of `/{id}/`: the
    /// `/me/` shape is the standard "current authenticated user" pattern
    /// used elsewhere in the codebase. Passing the agent's own id as a
    /// route param would tempt clients to send arbitrary ids and we'd
    /// have to enforce equality in code — `/me/` makes the constraint
    /// structural.
    /// </summary>
    [Route("api/agents")]
    [Authorize]
    public class AgentsController : BaseController
    {
        // Role list shared by every admin-tier endpoint on this
        // controller. Kept as a constant string so it stays consistent
        // across attributes — and because Roles attributes can't read
        // from C# arrays at compile time.
        private const string AdminRoles =
            "SuperAdmin,Admin,Partner,MarketplaceGrowthAssociate,TeamManager";

        private readonly IAgentProvisioningService _service;
        private readonly IAgentPayoutService _payouts;

        public AgentsController(
            IAgentProvisioningService service,
            IAgentPayoutService payouts)
        {
            _service = service;
            _payouts = payouts;
        }

        // ── Provisioning (existing) ────────────────────────────────

        [HttpGet]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result<List<AgentDetailsDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll() => ToActionResult(await _service.GetAllAsync());

        [HttpGet("{id:guid}")]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id) => ToActionResult(await _service.GetByIdAsync(id));

        [HttpPost]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateAgentRequest request)
        {
            if (request is null)
                return ToActionResult(Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Request is required."));
            return ToActionResult(await _service.CreateAsync(request));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentRequest request)
        {
            if (request is null)
                return ToActionResult(Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Request is required."));
            return ToActionResult(await _service.UpdateStatusAsync(id, request));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id) => ToActionResult(await _service.DeactivateAsync(id));

        [HttpPost("{id:guid}/reset-password")]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword(Guid id) => ToActionResult(await _service.ResetPasswordAsync(id));

        // ── Payouts — admin paths ──────────────────────────────────

        /// <summary>
        /// Records an external payment made to the agent. The system
        /// does NOT initiate a transfer — this is an audit-log entry
        /// that updates the outstanding balance.
        /// </summary>
        [HttpPost("{id:guid}/payouts")]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result<RecordAgentPayoutResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RecordPayout(
            Guid id,
            [FromBody] RecordAgentPayoutRequest request,
            CancellationToken ct)
        {
            if (request is null)
                return ToActionResult(Result<RecordAgentPayoutResponseDto>.Failure(
                    ErrorCodes.BadRequest, "Request is required."));

            var adminId = ResolveCurrentUserId();
            if (adminId is null)
                return ToActionResult(Result<RecordAgentPayoutResponseDto>.Failure(
                    ErrorCodes.Forbidden, "Could not resolve recording admin."));

            return ToActionResult(await _payouts.RecordPayoutAsync(id, request, adminId.Value, ct));
        }

        /// <summary>History + current summary for an agent. Admin path.</summary>
        [HttpGet("{id:guid}/payouts")]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result<AgentPayoutHistoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPayouts(
            Guid id,
            [FromQuery] int? take,
            CancellationToken ct)
            => ToActionResult(await _payouts.GetHistoryAsync(id, take ?? 100, ct));

        /// <summary>Earnings summary only (cheaper than the full history).</summary>
        [HttpGet("{id:guid}/earnings-summary")]
        [Authorize(Roles = AdminRoles)]
        [ProducesResponseType(typeof(Result<AgentEarningsSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEarningsSummary(Guid id, CancellationToken ct)
            => ToActionResult(await _payouts.GetSummaryAsync(id, ct));

        // ── Payouts — agent self-view paths ────────────────────────

        /// <summary>
        /// The signed-in agent's own payout history. Role-gated to
        /// `Agent` and scoped to the caller's id — no path parameter,
        /// no way for an agent to enumerate other agents' history.
        /// </summary>
        [HttpGet("me/payouts")]
        [Authorize(Roles = "Agent")]
        [ProducesResponseType(typeof(Result<AgentPayoutHistoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyPayouts(
            [FromQuery] int? take,
            CancellationToken ct)
        {
            var id = ResolveCurrentUserId();
            if (id is null)
                return ToActionResult(Result<AgentPayoutHistoryDto>.Failure(
                    ErrorCodes.Forbidden, "Could not resolve current agent."));
            return ToActionResult(await _payouts.GetHistoryAsync(id.Value, take ?? 100, ct));
        }

        /// <summary>The signed-in agent's own earnings summary.</summary>
        [HttpGet("me/earnings-summary")]
        [Authorize(Roles = "Agent")]
        [ProducesResponseType(typeof(Result<AgentEarningsSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyEarningsSummary(CancellationToken ct)
        {
            var id = ResolveCurrentUserId();
            if (id is null)
                return ToActionResult(Result<AgentEarningsSummaryDto>.Failure(
                    ErrorCodes.Forbidden, "Could not resolve current agent."));
            return ToActionResult(await _payouts.GetSummaryAsync(id.Value, ct));
        }

        // ── Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Resolves the caller's user id from the JWT claims. Identity
        /// emits the user id under `ClaimTypes.NameIdentifier` (default)
        /// — falling back to `sub` for tokens issued by upstream IdPs
        /// that follow the OIDC convention.
        /// </summary>
        private Guid? ResolveCurrentUserId()
        {
            var raw = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User?.FindFirstValue("sub")
                      ?? User?.FindFirstValue("nameid");
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }
}
