using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Agents.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Agents
{
    /// <summary>
    /// Service contract for agent operations.
    /// </summary>
    public interface IAgentService
    {
        /// <summary>
        /// Gets all agents.
        /// </summary>
        Task<Result<List<AgentListItemDto>>> GetAllAsync();

        /// <summary>
        /// Gets an agent by identifier.
        /// </summary>
        Task<Result<AgentDetailsDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// Creates a new agent.
        /// </summary>
        Task<Result<AgentDetailsDto>> CreateAsync(CreateAgentRequestDto request);

        /// <summary>
        /// Updates an existing agent.
        /// </summary>
        Task<Result<AgentDetailsDto>> UpdateAsync(Guid id, UpdateAgentRequestDto request);

        /// <summary>
        /// Deletes an agent.
        /// </summary>
        Task<Result> DeleteAsync(Guid id);
    }
}
