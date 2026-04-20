using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Domain.SellerLeads;

namespace ZansiHustle.Application.Persistence.SellerLeads
{
    /// <summary>
    /// Repository contract for managing seller lead persistence operations.
    /// </summary>
    public interface ISellerLeadRepository
    {
        /// <summary>
        /// Gets all seller leads in the system.
        /// </summary>
        /// <returns>A list of seller leads.</returns>
        Task<List<SellerLead>> GetAllAsync();

        /// <summary>
        /// Gets seller leads associated with a specific user (most recent first).
        /// Used by the merchant portal to detect whether the signed-in user has
        /// already submitted a seller application.
        /// </summary>
        /// <param name="userId">The owning user's id.</param>
        Task<List<SellerLead>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// Gets a single seller lead by its unique identifier.
        /// </summary>
        /// <param name="id">The seller lead identifier.</param>
        /// <returns>The matching seller lead if found; otherwise null.</returns>
        Task<SellerLead?> GetByIdAsync(Guid id);

        /// <summary>
        /// Finds the seller lead that produced a given merchant, or failing
        /// that, any lead submitted with the same email. Used by the seller
        /// portal to fall back onto richer onboarding-form data when the
        /// Merchant record's own fields are null (e.g. legacy onboarding
        /// captured into SellerLead; Merchant created later without copying
        /// every field).
        /// </summary>
        Task<SellerLead?> FindFallbackForMerchantAsync(Guid merchantId, string? email);

        /// <summary>
        /// Gets a single seller lead by its unique business code.
        /// </summary>
        /// <param name="code">The seller lead code.</param>
        /// <returns>The matching seller lead if found; otherwise null.</returns>
        Task<SellerLead?> GetByCodeAsync(string code);

        /// <summary>
        /// Checks whether a seller lead code already exists.
        /// </summary>
        /// <param name="code">The code to check.</param>
        /// <returns>True if the code exists; otherwise false.</returns>
        Task<bool> ExistsByCodeAsync(string code);

        /// <summary>
        /// Adds a new seller lead to the data store.
        /// </summary>
        /// <param name="sellerLead">The seller lead entity to add.</param>
        Task AddAsync(SellerLead sellerLead);

        /// <summary>
        /// Updates an existing seller lead in the data store.
        /// </summary>
        /// <param name="sellerLead">The seller lead entity to update.</param>
        void Update(SellerLead sellerLead);

        /// <summary>
        /// Deletes a seller lead from the data store.
        /// </summary>
        /// <param name="sellerLead">The seller lead entity to delete.</param>
        void Delete(SellerLead sellerLead);

        /// <summary>
        /// Persists all pending repository changes to the database.
        /// </summary>
        /// <returns>True if one or more records were affected; otherwise false.</returns>
        Task<bool> SaveChangesAsync();
    }
}
