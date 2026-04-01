using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.AgentApplications;

namespace ZansiHustle.Application.Persistence.AgentApplications
{
    /// <summary>
    /// Repository contract for managing agent application persistence operations.
    /// </summary>
    public interface IAgentApplicationRepository
    {
        /// <summary>
        /// Gets all agent applications in the system.
        /// </summary>
        /// <returns>A list of agent applications.</returns>
        Task<List<AgentApplication>> GetAllAsync();

        /// <summary>
        /// Gets a single agent application by its unique identifier.
        /// </summary>
        /// <param name="id">The agent application identifier.</param>
        /// <returns>The matching application if found; otherwise null.</returns>
        Task<AgentApplication?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a single agent application by its unique business code.
        /// </summary>
        /// <param name="code">The application code.</param>
        /// <returns>The matching application if found; otherwise null.</returns>
        Task<AgentApplication?> GetByCodeAsync(string code);

        /// <summary>
        /// Checks whether an application code already exists.
        /// </summary>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code exists; otherwise false.</returns>
        Task<bool> ExistsByCodeAsync(string code);

        /// <summary>
        /// Adds a new agent application to the data store.
        /// </summary>
        /// <param name="application">The application entity to add.</param>
        Task AddAsync(AgentApplication application);

        /// <summary>
        /// Updates an existing agent application in the data store.
        /// </summary>
        /// <param name="application">The application entity to update.</param>
        void Update(AgentApplication application);

        /// <summary>
        /// Deletes an agent application from the data store.
        /// </summary>
        /// <param name="application">The application entity to delete.</param>
        void Delete(AgentApplication application);

        /// <summary>
        /// Persists all pending repository changes to the database.
        /// </summary>
        /// <returns>True if one or more records were affected; otherwise false.</returns>
        Task<bool> SaveChangesAsync();
    }
}
