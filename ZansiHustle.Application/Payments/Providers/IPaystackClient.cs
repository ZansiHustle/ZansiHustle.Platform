using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments.Providers
{
    /// <summary>
    /// Thin abstraction over Paystack's REST API. Implementations handle
    /// authentication, serialization, retries, and webhook signature verification.
    /// </summary>
    public interface IPaystackClient
    {
        bool IsConfigured { get; }

        /// <summary>The configured Paystack public key — safe to return to mobile clients.</summary>
        string? PublicKey { get; }

        Task<Result<PaystackInitializeResponse>> InitializeTransactionAsync(
            PaystackInitializeRequest request,
            CancellationToken cancellationToken = default);

        Task<Result<PaystackVerifyResponse>> VerifyTransactionAsync(
            string reference,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Verifies the <c>x-paystack-signature</c> HMAC-SHA512 over the raw request body.
        /// Returns false if credentials are unconfigured or the header is missing.
        /// </summary>
        bool VerifyWebhookSignature(string rawBody, string? signatureHeader);
    }
}
