using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments.Providers
{
    /// <summary>
    /// Thin abstraction over Ozow's REST API. Implementations handle the
    /// token endpoint, the payment-create call, transaction lookup, and
    /// safe error/timeout behavior. Hash generation is delegated to
    /// <see cref="IOzowHashService"/>.
    /// </summary>
    public interface IOzowClient
    {
        /// <summary>
        /// True only when both API credentials and the hash service are
        /// ready. False short-circuits all calls to <see cref="ErrorCodes.ProviderNotConfigured"/>.
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// When true, payment initiation should treat this run as
        /// controlled UAT testing — cap the amount at <see cref="UatTestAmount"/>,
        /// prefix the local payment Code / TransactionReference with "UAT-TEST-",
        /// and persist <c>Payment.IsTest = true</c>. Sourced from
        /// <c>Ozow:UatTestMode</c>.
        /// </summary>
        bool UatTestMode { get; }

        /// <summary>Amount to use when <see cref="UatTestMode"/> is true (ZAR).</summary>
        decimal UatTestAmount { get; }

        /// <summary>
        /// POST <c>/token</c> (form-urlencoded). Most flows do NOT need
        /// this — PostPaymentRequest is authenticated by the static
        /// <c>ApiKey</c> header. Token is only required for certain
        /// merchant-management endpoints.
        /// </summary>
        Task<Result<OzowTokenResponse>> GetTokenAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// POST <c>/PostPaymentRequest</c>. Returns the redirect URL the
        /// buyer should be sent to in order to complete the EFT.
        /// </summary>
        Task<Result<OzowPaymentRequestResult>> CreatePaymentRequestAsync(
            OzowPaymentRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// GET <c>/GetTransactionByReference</c>. Used for server-side
        /// reconciliation when the webhook is delayed or the user returns
        /// to the app before NotifyUrl fires.
        /// </summary>
        Task<Result<OzowTransactionModel>> GetTransactionByReferenceAsync(
            string transactionReference,
            CancellationToken cancellationToken = default);

        /// <summary>GET <c>/GetTransaction</c> by Ozow transactionId.</summary>
        Task<Result<OzowTransactionModel>> GetTransactionAsync(
            string transactionId,
            CancellationToken cancellationToken = default);
    }
}
