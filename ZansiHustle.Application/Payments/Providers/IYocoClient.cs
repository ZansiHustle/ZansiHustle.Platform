using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments.Providers
{
    /// <summary>
    /// Thin abstraction over Yoco's Online Payments REST API.
    /// Implementations handle Bearer-token auth, JSON serialization,
    /// timeouts, and safe logging (no secret leakage).
    /// </summary>
    public interface IYocoClient
    {
        /// <summary>
        /// True only when both API credentials and the webhook signing
        /// secret are present. False short-circuits all calls to
        /// <see cref="ZansiHustle.Shared.Errors.ErrorCodes.ProviderNotConfigured"/>.
        /// </summary>
        bool IsConfigured { get; }

        /// <summary>
        /// When true, payment initiation should treat this run as
        /// controlled UAT testing. Mirrors <c>IOzowClient.UatTestMode</c>.
        /// </summary>
        bool UatTestMode { get; }

        /// <summary>Amount to use when <see cref="UatTestMode"/> is true (ZAR).</summary>
        decimal UatTestAmount { get; }

        /// <summary>POST <c>/checkouts</c>. Returns the hosted-checkout URL.</summary>
        Task<Result<YocoCheckoutResponse>> CreateCheckoutAsync(
            YocoCheckoutRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// GET <c>/checkouts/{id}</c>. Used for server-side reconciliation
        /// when the webhook is delayed or the buyer returns to the app
        /// before the event is delivered.
        /// </summary>
        Task<Result<YocoCheckoutResponse>> GetCheckoutAsync(
            string checkoutId,
            CancellationToken cancellationToken = default);
    }
}
