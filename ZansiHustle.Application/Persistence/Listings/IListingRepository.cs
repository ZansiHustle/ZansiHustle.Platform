using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Domain.Listings;

namespace ZansiHustle.Application.Persistence.Listings
{
    /// <summary>
    /// Persistence contract for <see cref="Listing"/>.
    /// </summary>
    public interface IListingRepository
    {
        Task<(List<Listing> Items, int Total)> SearchAsync(ListingFilterRequestDto filter);
        Task<Listing?> GetByIdAsync(Guid id);
        Task<Listing?> GetBySlugAsync(string slug);
        Task<List<Listing>> GetByMerchantAsync(Guid merchantId);

        /// <summary>
        /// Returns listings explicitly attached to a ShopProfile —
        /// <c>ListingSource == ShopProfile</c> AND
        /// <c>ShopProfileId == shopProfileId</c>. The public
        /// ShopProfile page uses this so SellerAccount listings under
        /// the same merchant don't appear on the shop's catalog.
        /// </summary>
        Task<List<Listing>> GetByShopProfileAsync(Guid shopProfileId);

        Task<List<Listing>> GetByOwnerAsync(Guid ownerUserId);

        /// <summary>
        /// Looks up a mirrored Listing by its external-catalog identity
        /// (ExternalSourceCode, ExternalProductId) — the sync upsert key.
        /// Eager-loads Variants (same shape as <see cref="GetByIdAsync"/>)
        /// so the sync writer can mutate the tracked graph directly.
        /// </summary>
        Task<Listing?> GetByExternalIdAsync(string sourceCode, string externalProductId);

        Task<bool> ExistsBySlugAsync(string slug);
        Task AddAsync(Listing listing);
        void Update(Listing listing);
        void Delete(Listing listing);
        Task<bool> SaveChangesAsync();

        /// <summary>
        /// Saves, then clears the context's change tracker. Use after a save
        /// that mutated a collection navigation (e.g. adding a brand-new
        /// child variant to an already-loaded Listing) right before another
        /// unrelated save runs on the same DbContext — leaves nothing stale
        /// tracked for that next save to trip over. Mirrors the tracker-clear
        /// SaveListingAndReplaceVariantsAsync already does for its own
        /// variant-graph reasons.
        /// </summary>
        Task<bool> SaveChangesAndDetachAsync();

        /// <summary>
        /// Adds brand-new variant rows for an EXISTING listing, as their own
        /// operation separate from any scalar/child mutation already staged
        /// on that listing's tracked graph. Used by external-catalog sync's
        /// stable-id upsert: updates to already-tracked existing variants are
        /// saved first, new variants are added and saved in a distinct pass —
        /// simpler to reason about than one mixed update+insert SaveChanges
        /// call against a graph loaded via Include.
        /// </summary>
        Task AddVariantsAsync(IEnumerable<ListingVariant> variants);

        /// <summary>
        /// Atomic listing-update + variant-replace operation. Wraps:
        ///   1. Detaching the listing's currently-tracked variants
        ///      (so the change tracker doesn't fight us during the
        ///      DB-side delete).
        ///   2. <c>ExecuteDeleteAsync</c> against ListingVariants for
        ///      this listing — removes every existing variant in a
        ///      single SQL statement, no row-by-row tracking.
        ///   3. <c>AddRangeAsync</c> on the new variant entities (fresh
        ///      server-issued PKs).
        ///   4. <c>SaveChangesAsync</c> committing the listing's scalar
        ///      edits and the new variant INSERTs in the same DB tx.
        ///
        /// All four steps run inside <c>BeginTransactionAsync</c> so a
        /// failure at any point rolls the listing back to its
        /// pre-request state — no half-replaced variant sets.
        ///
        /// Contract for <paramref name="newVariantsOrNull"/>:
        ///   • <c>null</c>  → variant set untouched, only the listing's
        ///                    scalar changes are committed.
        ///   • <c>[]</c>    → ALL existing variants are deleted.
        ///   • non-empty    → existing variants deleted, supplied
        ///                    variants inserted with their pre-assigned
        ///                    <c>Id</c>s. Client variant ids are NOT
        ///                    preserved across an update — the service
        ///                    layer assigns fresh GUIDs before calling.
        /// </summary>
        Task<bool> SaveListingAndReplaceVariantsAsync(
            Listing listing,
            IReadOnlyList<ListingVariant>? newVariantsOrNull);

        /// <summary>
        /// Full-snapshot archive boundary: archives (Status = Archived,
        /// variants deactivated) every Listing for <paramref name="sourceCode"/>
        /// that was NOT touched by <paramref name="syncRunId"/> — i.e. it
        /// existed before this run but the run's snapshot no longer
        /// includes it. Never touches Listings for a different source, and
        /// never touches manually-created (ExternalSourceCode == null)
        /// Listings — both are excluded by the WHERE clause itself, not by
        /// caller discipline. Returns the number of Listings archived.
        /// Bulk column updates via ExecuteUpdateAsync — no entity loading.
        /// </summary>
        Task<int> ArchiveUntouchedExternalListingsAsync(string sourceCode, Guid syncRunId);
    }
}
