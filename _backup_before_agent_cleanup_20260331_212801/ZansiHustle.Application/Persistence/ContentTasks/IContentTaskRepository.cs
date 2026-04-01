using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.ContentTasks;

namespace ZansiHustle.Application.Persistence.ContentTasks
{
    /// <summary>
    /// Repository contract for managing content task persistence operations.
    /// </summary>
    public interface IContentTaskRepository
    {
        /// <summary>
        /// Gets all content tasks in the system.
        /// </summary>
        /// <returns>A list of content tasks.</returns>
        Task<List<ContentTask>> GetAllAsync();

        /// <summary>
        /// Gets a single content task by its unique identifier.
        /// </summary>
        /// <param name="id">The content task identifier.</param>
        /// <returns>The matching content task if found; otherwise null.</returns>
        Task<ContentTask?> GetByIdAsync(Guid id);

        /// <summary>
        /// Adds a new content task to the data store.
        /// </summary>
        /// <param name="contentTask">The content task entity to add.</param>
        Task AddAsync(ContentTask contentTask);

        /// <summary>
        /// Updates an existing content task in the data store.
        /// </summary>
        /// <param name="contentTask">The content task entity to update.</param>
        void Update(ContentTask contentTask);

        /// <summary>
        /// Deletes a content task from the data store.
        /// </summary>
        /// <param name="contentTask">The content task entity to delete.</param>
        void Delete(ContentTask contentTask);

        /// <summary>
        /// Persists all pending repository changes to the database.
        /// </summary>
        /// <returns>True if one or more records were affected; otherwise false.</returns>
        Task<bool> SaveChangesAsync();
    }
}
