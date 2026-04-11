using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.AgentApplications.Dtos;
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
        Task<Result<AgentApplicationDetailsDto>> ReviewAsync(Guid id, ReviewAgentApplicationRequestDto request);
        Task<Result> DeleteAsync(Guid id);
    }
}

