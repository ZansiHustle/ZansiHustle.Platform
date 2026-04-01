using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.Campaigns;

namespace ZansiHustle.Application.Persistence.Campaigns
{
    /// <summary>
    /// Repository contract for managing campaign persistence operations.
    /// </summary>
    public interface ICampaignRepository
    {
        /// <summary>
        /// Gets all campaigns in the system.
        /// </summary>
        /// <returns>A list of campaigns.</returns>
        Task<List<Campaign>> GetAllAsync();

        /// <summary>
        /// Gets a single campaign by its unique identifier.
        /// </summary>
        /// <param name="id">The campaign identifier.</param>
        /// <returns>The matching campaign if found; otherwise null.</returns>
        Task<Campaign?> GetByIdAsync(Guid id);

        /// <summary>
        /// Gets a single campaign by its unique business code.
        /// </summary>
        /// <param name="code">The campaign code.</param>
        /// <returns>The matching campaign if found; otherwise null.</returns>
        Task<Campaign?> GetByCodeAsync(string code);

        /// <summary>
        /// Checks whether a campaign code already exists.
        /// </summary>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code exists; otherwise false.</returns>
        Task<bool> ExistsByCodeAsync(string code);

        /// <summary>
        /// Adds a new campaign to the data store.
        /// </summary>
        /// <param name="campaign">The campaign entity to add.</param>
        Task AddAsync(Campaign campaign);

        /// <summary>
        /// Updates an existing campaign in the data store.
        /// </summary>
        /// <param name="campaign">The campaign entity to update.</param>
        void Update(Campaign campaign);

        /// <summary>
        /// Deletes a campaign from the data store.
        /// </summary>
        /// <param name="campaign">The campaign entity to delete.</param>
        void Delete(Campaign campaign);

        /// <summary>
        /// Persists all pending repository changes to the database.
        /// </summary>
        /// <returns>True if one or more records were affected; otherwise false.</returns>
        Task<bool> SaveChangesAsync();
    }
}
