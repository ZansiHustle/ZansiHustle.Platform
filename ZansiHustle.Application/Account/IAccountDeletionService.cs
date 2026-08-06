using System;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Account
{
    /// <summary>
    /// Self-service account deletion (App Store Guideline 5.1.1(v)).
    ///
    /// ZansiHustle carries immutable financial records (orders, payments, wallet
    /// ledgers, seller/agent payouts) that cannot be hard-deleted for legal /
    /// accounting reasons. This service therefore performs the Apple-accepted
    /// "make the account permanently inaccessible + anonymise personal data"
    /// deletion: it strips every piece of personal information, removes the
    /// user's public content from discovery, revokes all sessions, and flips the
    /// account to <c>AccountStatus.Deleted</c> — which the auth layer already
    /// rejects at login/refresh, so the account can never be signed into again.
    /// </summary>
    public interface IAccountDeletionService
    {
        /// <summary>
        /// Permanently deletes (anonymises + deactivates) the caller's own
        /// account. Idempotent: deleting an already-deleted account succeeds.
        /// </summary>
        /// <param name="userId">The authenticated caller's user id.</param>
        /// <param name="reason">Optional free-text reason for analytics only.</param>
        Task<Result> DeleteMyAccountAsync(Guid userId, string? reason);
    }
}
