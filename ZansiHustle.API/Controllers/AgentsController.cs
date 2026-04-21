using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Agents.AgentApplications;
using ZansiHustle.Application.Agents.AgentApplications.Dtos;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Shared.Enums.AgentApplications;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Admin-facing endpoints for managing active agents.
    ///
    /// Under the hood, an "agent" is an <see cref="AgentApplication"/>
    /// row — the legacy dedicated Agents table was dropped and agents
    /// were unified with applications. An <b>approved</b> application
    /// IS an active agent; a <b>rejected</b> one is a suspended agent.
    /// Separating the two pages (agents vs applications) is purely a
    /// UX convention — this controller surfaces the approved /
    /// rejected subset; AgentApplicationsController surfaces pending
    /// applications.
    ///
    /// Status translation: the portal uses 1=Active, 3=Suspended,
    /// 4=Inactive for an "agent" list. This collides numerically with
    /// <see cref="AgentApplicationStatus"/> (Pending=1, Approved=2,
    /// Rejected=3, UnderReview=4), so we translate in both directions
    /// here. Callers never see application-status internals.
    /// </summary>
    [Route("api/agents")]
    [Authorize]
    public class AgentsController : BaseController
    {
        private readonly IAgentApplicationService _service;
        private readonly ICurrentUserService _currentUser;

        public AgentsController(IAgentApplicationService service, ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        /// <summary>Lists the active + suspended agents (excludes Pending / UnderReview).</summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<List<AgentListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            if (!result.IsSuccess)
                return ToActionResult(Result<List<AgentListItemDto>>.Failure(result.Code, result.Message));

            var agents = (result.Data ?? new List<AgentApplicationListItemDto>())
                .Where(a => a.Status == AgentApplicationStatus.Approved
                         || a.Status == AgentApplicationStatus.Rejected)
                .Select(ToAgentListItem)
                .ToList();

            return ToActionResult(Result<List<AgentListItemDto>>.Success(agents, result.Message));
        }

        /// <summary>Fetches a single agent by id.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (!result.IsSuccess)
                return ToActionResult(Result<AgentDetailsDto>.Failure(result.Code, result.Message));

            return ToActionResult(Result<AgentDetailsDto>.Success(ToAgentDetails(result.Data!), result.Message));
        }

        /// <summary>
        /// Admin-creates an agent. The incoming status (1/3/4) is
        /// translated to an AgentApplicationStatus; default Active.
        /// The created application is stamped as reviewed by the
        /// calling admin so the audit trail reflects who added them.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateAgentRequestDto request)
        {
            if (request is null)
                return ToActionResult(Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Request is required."));

            // Default to Active when the caller didn't supply a status —
            // the admin adding an agent rarely wants them suspended on
            // day one, and the portal's drawer defaults to "active" too.
            var internalStatus = FromPortalStatus(request.Status ?? 1);

            var result = await _service.AdminCreateAsync(
                new CreateAgentApplicationRequestDto
                {
                    FullName = request.FullName,
                    PhoneNumber = request.PhoneNumber,
                    Email = request.Email,
                    Province = request.Province,
                    City = request.City,
                    SocialHandle = request.SocialHandle,
                    Notes = request.Notes,
                },
                internalStatus,
                _currentUser.UserId);

            if (!result.IsSuccess)
                return ToActionResult(Result<AgentDetailsDto>.Failure(result.Code, result.Message));

            return ToActionResult(Result<AgentDetailsDto>.Success(ToAgentDetails(result.Data!), result.Message));
        }

        /// <summary>
        /// Updates the agent's status (Active / Suspended / Inactive).
        /// Thin wrapper over ReviewAsync; keeps the seller-portal's
        /// `PUT /api/agents/{id}` contract working transparently.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<AgentDetailsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentRequestDto request)
        {
            if (request is null)
                return ToActionResult(Result<AgentDetailsDto>.Failure(ErrorCodes.BadRequest, "Request is required."));

            var internalStatus = FromPortalStatus(request.Status ?? 1);

            var result = await _service.ReviewAsync(id, new ReviewAgentApplicationRequestDto
            {
                Status = internalStatus,
                Notes = request.Notes,
                ReviewedByUserId = _currentUser.UserId,
            });

            if (!result.IsSuccess)
                return ToActionResult(Result<AgentDetailsDto>.Failure(result.Code, result.Message));

            return ToActionResult(Result<AgentDetailsDto>.Success(ToAgentDetails(result.Data!), result.Message));
        }

        /// <summary>Delete / remove an agent record entirely.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return ToActionResult(result);
        }

        // ── Status translation ────────────────────────────────────
        // Portal encoding:   1 = Active, 3 = Suspended, 4 = Inactive
        // Internal enum:     Approved = 2, Rejected = 3, UnderReview = 4

        private static AgentApplicationStatus FromPortalStatus(int s) => s switch
        {
            1 => AgentApplicationStatus.Approved,
            3 => AgentApplicationStatus.Rejected,
            _ => AgentApplicationStatus.UnderReview,
        };

        private static int ToPortalStatus(AgentApplicationStatus s) => s switch
        {
            AgentApplicationStatus.Approved => 1,
            AgentApplicationStatus.Rejected => 3,
            _ => 4,
        };

        private static AgentListItemDto ToAgentListItem(AgentApplicationListItemDto a) => new()
        {
            Id = a.Id,
            Code = a.Code,
            FullName = a.FullName,
            PhoneNumber = a.PhoneNumber,
            Email = a.Email,
            Province = a.Province,
            City = a.City,
            Status = ToPortalStatus(a.Status),
            JoinedDateUtc = a.SubmittedAtUtc,
        };

        private static AgentDetailsDto ToAgentDetails(AgentApplicationDetailsDto a) => new()
        {
            Id = a.Id,
            Code = a.Code,
            FullName = a.FullName,
            PhoneNumber = a.PhoneNumber,
            Email = a.Email,
            Province = a.Province,
            City = a.City,
            SocialHandle = a.SocialHandle,
            Notes = a.Notes,
            Status = ToPortalStatus(a.Status),
            JoinedDateUtc = a.SubmittedAtUtc,
            UpdatedAtUtc = a.UpdatedAtUtc,
        };
    }

    // ── Request / response DTOs scoped to this controller ─────────

    public class CreateAgentRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SocialHandle { get; set; }
        public string? Notes { get; set; }
        /// <summary>Portal encoding: 1=Active (default), 3=Suspended, 4=Inactive.</summary>
        public int? Status { get; set; }
    }

    public class UpdateAgentRequestDto
    {
        /// <summary>Portal encoding: 1=Active, 3=Suspended, 4=Inactive.</summary>
        public int? Status { get; set; }
        public string? Notes { get; set; }
    }

    public class AgentListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        /// <summary>Portal encoding: 1=Active, 3=Suspended, 4=Inactive.</summary>
        public int Status { get; set; }
        public DateTime JoinedDateUtc { get; set; }
    }

    public class AgentDetailsDto : AgentListItemDto
    {
        public string? SocialHandle { get; set; }
        public string? Notes { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
