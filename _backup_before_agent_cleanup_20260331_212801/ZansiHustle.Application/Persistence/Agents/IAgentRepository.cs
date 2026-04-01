using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Agents;

namespace ZansiHustle.Application.Persistence.Agents
{
    /// <summary>
    /// Repository contract for managing agent persistence operations.
    /// </summary>
    public interface IAgentRepository
    {
        /// <summary>
        /// Gets all agents in the system.
        /// </summary>
        /// <returns>A list of agents.</returns>
        Task<List<Agent>> GetAllAsync();

        /// <summary>
        /// Gets a single agent by its unique identifier.
        /// </summary>
        /// <param name="id">The agent identifier.</param>
        /// <returns>The matching agent if found; otherwise null.</returns>
        Task<Agent?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a single agent by its unique business code.
        /// </summary>
        /// <param name="code">The agent code.</param>
        /// <returns>The matching agent if found; otherwise null.</returns>
        Task<Agent?> GetByCodeAsync(string code);

        /// <summary>
        /// Checks whether an agent code already exists.
        /// </summary>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code exists; otherwise false.</returns>
        Task<bool> ExistsByCodeAsync(string code);

        /// <summary>
        /// Adds a new agent to the data store.
        /// </summary>
        /// <param name="agent">The agent entity to add.</param>
        Task AddAsync(Agent agent);

        /// <summary>
        /// Updates an existing agent in the data store.
        /// </summary>
        /// <param name="agent">The agent entity to update.</param>
        void Update(Agent agent);

        /// <summary>
        /// Deletes an agent from the data store.
        /// </summary>
        /// <param name="agent">The agent entity to delete.</param>
        void Delete(Agent agent);

        /// <summary>
        /// Persists all pending repository changes to the database.
        /// </summary>
        /// <returns>True if one or more records were affected; otherwise false.</returns>
        Task<bool> SaveChangesAsync();
    }
}
