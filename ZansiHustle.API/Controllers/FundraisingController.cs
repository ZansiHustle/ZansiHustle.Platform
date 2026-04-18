using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Fundraising;
using ZansiHustle.Application.Fundraising.Dtos;
using ZansiHustle.Shared.Enums.Fundraising;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Private fundraising module — admin-controlled investment simulator and
    /// stakeholder tracking. Not a public securities marketplace; all projections
    /// are clearly labeled scenarios, never guarantees.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize(Roles = RoleRead)]
    public class FundraisingController : BaseController
    {
        // Readers: can view active valuation, cap table, run simulator.
        private const string RoleRead = "SuperAdmin,Admin,Partner,Accountant";

        // Writers: can mutate stakeholders.
        private const string RoleStakeholderWrite = "SuperAdmin,Admin,Partner";

        // Full admin: full valuation history + CRUD on valuations.
        private const string RoleFullAdmin = "SuperAdmin,Admin";

        private readonly IFundraisingService _service;
        private readonly ICurrentUserService _currentUserService;

        public FundraisingController(IFundraisingService service, ICurrentUserService currentUserService)
        {
            _service = service;
            _currentUserService = currentUserService;
        }

        // ── Valuations ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the active valuation (partner-safe view — excludes InternalBaseline).
        /// </summary>
        [HttpGet("valuations/active")]
        [ProducesResponseType(typeof(Result<ActiveValuationPublicDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveValuation()
        {
            var result = await _service.GetActivePublicAsync();
            return ToActionResult(result);
        }

        /// <summary>
        /// Returns the full active valuation record (Admin/SuperAdmin only — includes InternalBaseline).
        /// </summary>
        [HttpGet("valuations/active/full")]
        [Authorize(Roles = RoleFullAdmin)]
        [ProducesResponseType(typeof(Result<ValuationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetActiveValuationFull()
        {
            var result = await _service.GetActiveValuationAsync();
            return ToActionResult(result);
        }

        /// <summary>
        /// Valuation history (Admin/SuperAdmin only — includes InternalBaseline on each record).
        /// </summary>
        [HttpGet("valuations")]
        [Authorize(Roles = RoleFullAdmin)]
        [ProducesResponseType(typeof(Result<List<ValuationDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetValuations()
        {
            var result = await _service.GetAllValuationsAsync();
            return ToActionResult(result);
        }

        [HttpGet("valuations/{id:guid}")]
        [Authorize(Roles = RoleFullAdmin)]
        [ProducesResponseType(typeof(Result<ValuationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetValuationById(Guid id)
        {
            var result = await _service.GetValuationByIdAsync(id);
            return ToActionResult(result);
        }

        [HttpPost("valuations")]
        [Authorize(Roles = RoleFullAdmin)]
        [ProducesResponseType(typeof(Result<ValuationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateValuation([FromBody] CreateValuationRequestDto request)
        {
            var result = await _service.CreateValuationAsync(_currentUserService.UserId, request);
            return ToActionResult(result);
        }

        [HttpPut("valuations/{id:guid}")]
        [Authorize(Roles = RoleFullAdmin)]
        [ProducesResponseType(typeof(Result<ValuationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateValuation(Guid id, [FromBody] UpdateValuationRequestDto request)
        {
            var result = await _service.UpdateValuationAsync(id, request);
            return ToActionResult(result);
        }

        [HttpPost("valuations/{id:guid}/activate")]
        [Authorize(Roles = RoleFullAdmin)]
        [ProducesResponseType(typeof(Result<ValuationDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ActivateValuation(Guid id)
        {
            var result = await _service.ActivateValuationAsync(id);
            return ToActionResult(result);
        }

        [HttpDelete("valuations/{id:guid}")]
        [Authorize(Roles = RoleFullAdmin)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteValuation(Guid id)
        {
            var result = await _service.DeleteValuationAsync(id);
            return ToActionResult(result);
        }

        // ── Stakeholders ────────────────────────────────────────────────

        /// <summary>Lists stakeholders. Accepts optional type + activeOnly filter.</summary>
        [HttpGet("stakeholders")]
        [ProducesResponseType(typeof(Result<List<StakeholderDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStakeholders([FromQuery] StakeholderType? type, [FromQuery] bool activeOnly = false)
        {
            var result = await _service.GetStakeholdersAsync(type, activeOnly);
            return ToActionResult(result);
        }

        [HttpGet("stakeholders/{id:guid}")]
        [ProducesResponseType(typeof(Result<StakeholderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStakeholderById(Guid id)
        {
            var result = await _service.GetStakeholderByIdAsync(id);
            return ToActionResult(result);
        }

        [HttpPost("stakeholders")]
        [Authorize(Roles = RoleStakeholderWrite)]
        [ProducesResponseType(typeof(Result<StakeholderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateStakeholder([FromBody] CreateStakeholderRequestDto request)
        {
            var result = await _service.CreateStakeholderAsync(_currentUserService.UserId, request);
            return ToActionResult(result);
        }

        [HttpPut("stakeholders/{id:guid}")]
        [Authorize(Roles = RoleStakeholderWrite)]
        [ProducesResponseType(typeof(Result<StakeholderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStakeholder(Guid id, [FromBody] UpdateStakeholderRequestDto request)
        {
            var result = await _service.UpdateStakeholderAsync(id, request);
            return ToActionResult(result);
        }

        /// <summary>Soft-deactivates a stakeholder (IsActive = false).</summary>
        [HttpDelete("stakeholders/{id:guid}")]
        [Authorize(Roles = RoleStakeholderWrite)]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeactivateStakeholder(Guid id)
        {
            var result = await _service.DeactivateStakeholderAsync(id);
            return ToActionResult(result);
        }

        // ── Simulator + summary ─────────────────────────────────────────

        /// <summary>
        /// Runs an investment projection using the currently active fundraising
        /// valuation. Returns ownership % and scenario-based projected values.
        /// All projections are illustrative — never guarantees.
        /// </summary>
        [HttpPost("simulate")]
        [ProducesResponseType(typeof(Result<SimulateResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Simulate([FromBody] SimulateRequestDto request)
        {
            var result = await _service.SimulateAsync(request);
            return ToActionResult(result);
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(Result<FundraisingSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _service.GetSummaryAsync();
            return ToActionResult(result);
        }
    }
}
