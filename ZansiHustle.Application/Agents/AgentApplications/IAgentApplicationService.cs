using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.AgentApplications.Dtos;
using ZansiHustle.Shared.Enums.AgentApplications;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Agents.AgentApplications
{
    /// <summary>
    /// Service contract for user application operations.
    /// </summary>
    public interface IAgentApplicationService
    {
        Task<Result<List<AgentApplicationListItemDto>>> GetAllAsync();
        Task<Result<AgentApplicationDetailsDto>> GetByIdAsync(Guid id);
        Task<Result<AgentApplicationDetailsDto>> CreateAsync(CreateAgentApplicationRequestDto request);

        /// <summary>
        /// Admin-side shortcut: create an application AND immediately stamp
        /// it with a chosen status (typically Approved, so the new row
        /// represents an active agent). Used by /api/agents POST so the
        /// Launch Ops admin can add an agent in one step without an
        /// intermediate "review" round-trip.
        /// </summary>
        Task<Result<AgentApplicationDetailsDto>> AdminCreateAsync(
            CreateAgentApplicationRequestDto request,
            AgentApplicationStatus initialStatus,
            Guid? reviewedByUserId);

        Task<Result<AgentApplicationDetailsDto>> ReviewAsync(Guid id, ReviewAgentApplicationRequestDto request);
        Task<Result> DeleteAsync(Guid id);
    }
}

