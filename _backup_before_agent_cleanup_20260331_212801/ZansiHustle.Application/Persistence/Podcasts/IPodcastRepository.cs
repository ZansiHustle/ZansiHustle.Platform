using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Podcasts;

namespace ZansiHustle.Application.Persistence.Podcasts
{
    /// <summary>
    /// Repository contract for managing podcast persistence operations.
    /// </summary>
    public interface IPodcastRepository
    {
        /// <summary>
        /// Gets all podcasts in the system.
        /// </summary>
        /// <returns>A list of podcasts.</returns>
        Task<List<Podcast>> GetAllAsync();

        /// <summary>
        /// Gets a single podcast by its unique identifier.
        /// </summary>
        /// <param name="id">The podcast identifier.</param>
        /// <returns>The matching podcast if found; otherwise null.</returns>
        Task<Podcast?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a single podcast by its unique business code.
        /// </summary>
        /// <param name="code">The podcast code.</param>
        /// <returns>The matching podcast if found; otherwise null.</returns>
        Task<Podcast?> GetByCodeAsync(string code);

        /// <summary>
        /// Checks whether a podcast code already exists.
        /// </summary>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code exists; otherwise false.</returns>
        Task<bool> ExistsByCodeAsync(string code);

        /// <summary>
        /// Adds a new podcast to the data store.
        /// </summary>
        /// <param name="podcast">The podcast entity to add.</param>
        Task AddAsync(Podcast podcast);

        /// <summary>
        /// Updates an existing podcast in the data store.
        /// </summary>
        /// <param name="podcast">The podcast entity to update.</param>
        void Update(Podcast podcast);

        /// <summary>
        /// Deletes a podcast from the data store.
        /// </summary>
        /// <param name="podcast">The podcast entity to delete.</param>
        void Delete(Podcast podcast);

        /// <summary>
        /// Persists all pending repository changes to the database.
        /// </summary>
        /// <returns>True if one or more records were affected; otherwise false.</returns>
        Task<bool> SaveChangesAsync();
    }
}
