using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Agents.AgentProvisioning;
using ZansiHustle.Application.Agents.AgentProvisioning.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin provisioning for agents. Each agent is a real IdentityUser
    /// in the "Agent" role with login credentials — NOT an
    /// AgentApplication record (that entity is reserved for the
    /// self-apply / public application workflow).
    ///
    /// Password handling: created / reset responses include a plaintext
    /// `initialPassword` ONE TIME so the admin can relay it to the
    /// agent. Subsequent GETs never include it. If admin loses the
    /// password they hit `POST /api/agents/{id}/reset-password` to
    /// generate a fresh one — the old one is invalidated server-side.
    /// </summary>
    [Route("api/agents")]
    // Provisioning + password regeneration are admin-tier operations
    // — extended to MarketplaceGrowthAssociate + TeamManager because
    // agent management is the operational responsibility of the
    // Marketplace Growth role (they own the agent pipeline).
    //
    // A signed-in Agent must NEVER reach these endpoints — that would
    // let an agent enumerate / mutate / reset other agents' accounts.
    // Buyers and Merchants are likewise blocked. This is enforced by
    // role allow-list rather than block-list because the shape of
    // "who can be inside the system" widens over time and the
    // explicit allow-list is the safer default.
    [Authorize(Roles = "SuperAdmin,Admin,Partner,MarketplaceGrowthAssociate,TeamManager")]
    public class AgentsController : BaseController
    {
        private readonly IAgentProvisioningService _service;

        public AgentsController(IAgentProvisioningService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(Result<List<AgentDetailsDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll() => ToActionResult(await _service.GetAllAsync());

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id) => ToActionResult(await _service.GetByIdAsync(id));

        [HttpPost]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateAgentRequest request)
        {
            if (request is null)
                return ToActionResult(Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Request is required."));
            return ToActionResult(await _service.CreateAsync(request));
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentRequest request)
        {
            if (request is null)
                return ToActionResult(Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Request is required."));
            return ToActionResult(await _service.UpdateStatusAsync(id, request));
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id) => ToActionResult(await _service.DeactivateAsync(id));

        [HttpPost("{id:guid}/reset-password")]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResetPassword(Guid id) => ToActionResult(await _service.ResetPasswordAsync(id));
    }
}
