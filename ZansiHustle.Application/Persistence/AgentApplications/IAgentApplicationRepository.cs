using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Agents.AgentApplications;

namespace ZansiHustle.Application.Persistence.AgentApplications
{
    /// <summary>
    /// Repository contract for managing user application persistence operations.
    /// </summary>
    public interface IAgentApplicationRepository
    {
        /// <summary>
        /// Gets all user applications in the system.
        /// </summary>
        /// <returns>A list of user applications.</returns>
        Task<List<AgentApplication>> GetAllAsync();

        /// <summary>
        /// Gets a single user application by its unique identifier.
        /// </summary>
        /// <param name="id">The user application identifier.</param>
        /// <returns>The matching application if found; otherwise null.</returns>
        Task<AgentApplication?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a single user application by its unique business code.
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
        /// Adds a new user application to the data store.
        /// </summary>
        /// <param name="application">The application entity to add.</param>
        Task AddAsync(AgentApplication application);

        /// <summary>
        /// Updates an existing user application in the data store.
        /// </summary>
        /// <param name="application">The application entity to update.</param>
        void Update(AgentApplication application);

        /// <summary>
        /// Deletes an user application from the data store.
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

