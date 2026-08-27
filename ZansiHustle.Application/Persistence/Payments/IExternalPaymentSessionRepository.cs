using System;
using System.Threading.Tasks;
using ZansiHustle.Domain.Payments.External;

namespace ZansiHustle.Application.Persistence.Payments
{
    public interface IExternalPaymentSessionRepository
    {
        Task<ExternalPaymentSession?> GetByIdAsync(Guid id);

        Task<ExternalPaymentSession?> GetByProviderReferenceAsync(string providerReference);

        /// <summary>
        /// Returns the most recent non-terminal-failed session (i.e. anything
        /// other than Failed/Cancelled/Expired) for this shop + external order
        /// — the idempotency lookup used by CreateSessionAsync. A prior
        /// failure never blocks a fresh retry; a Pending/RedirectCreated/
        /// Processing/Paid row does.
        /// </summary>
        Task<ExternalPaymentSession?> GetActiveByShopAndExternalOrderAsync(string shopCode, string externalOrderId);

        Task AddAsync(ExternalPaymentSession session);
        void Update(ExternalPaymentSession session);
        Task<bool> SaveChangesAsync();
    }
}
