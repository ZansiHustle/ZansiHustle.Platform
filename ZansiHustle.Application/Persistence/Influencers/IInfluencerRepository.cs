using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Influencers;

namespace ZansiHustle.Application.Persistence.Influencers
{
    /// <summary>
    /// Repository contract for managing influencer persistence operations.
    /// </summary>
    public interface IInfluencerRepository
    {
        /// <summary>
        /// Gets all influencers in the system.
        /// </summary>
        /// <returns>A list of influencers.</returns>
        Task<List<Influencer>> GetAllAsync();

        /// <summary>
        /// Gets a single influencer by its unique identifier.
        /// </summary>
        /// <param name="id">The influencer identifier.</param>
        /// <returns>The matching influencer if found; otherwise null.</returns>
        Task<Influencer?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a single influencer by its unique business code.
        /// </summary>
        /// <param name="code">The influencer code.</param>
        /// <returns>The matching influencer if found; otherwise null.</returns>
        Task<Influencer?> GetByCodeAsync(string code);

        /// <summary>
        /// Checks whether an influencer code already exists.
        /// </summary>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code exists; otherwise false.</returns>
        Task<bool> ExistsByCodeAsync(string code);

        /// <summary>
        /// Adds a new influencer to the data store.
        /// </summary>
        /// <param name="influencer">The influencer entity to add.</param>
        Task AddAsync(Influencer influencer);

        /// <summary>
        /// Updates an existing influencer in the data store.
        /// </summary>
        /// <param name="influencer">The influencer entity to update.</param>
        void Update(Influencer influencer);

        /// <summary>
        /// Deletes an influencer from the data store.
        /// </summary>
        /// <param name="influencer">The influencer entity to delete.</param>
        void Delete(Influencer influencer);

        /// <summary>
        /// Persists all pending repository changes to the database.
        /// </summary>
        /// <returns>True if one or more records were affected; otherwise false.</returns>
        Task<bool> SaveChangesAsync();

        Task<bool> SaveChangesTrackingAsync();

        Task<Influencer?> GetByIdNoTrackingAsync(Guid id);
        void DetachEntity(Influencer entity);

        Task<Influencer?> GetByIdForUpdateAsync(Guid id);
        void RemovePlatformAccount(InfluencerPlatformAccount account);
    }
}
