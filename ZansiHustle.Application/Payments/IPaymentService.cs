using System;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Payments.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments
{
    public interface IPaymentService
    {
        /// <summary>
        /// Initializes a payment attempt for the given order owned by <paramref name="buyerUserId"/>.
        /// Reuses any existing non-terminal attempt for the order so retries are idempotent.
        /// </summary>
        Task<Result<InitializePaymentResponseDto>> InitializeAsync(
            Guid buyerUserId,
            InitializePaymentRequestDto request,
            CancellationToken cancellationToken = default);

        Task<Result<PaymentDto>> GetByIdAsync(Guid userId, Guid paymentId);

        /// <summary>
        /// Server-side verification against the provider by reference. Useful when the
        /// webhook is delayed or the mobile client returns to the app and wants a
        /// synchronous confirmation.
        /// </summary>
        Task<Result<PaymentDto>> VerifyAsync(Guid userId, string reference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a webhook payload. Always completes (even on signature mismatch)
        /// by persisting an audit row; actionable events are applied only if signature
        /// + dedup key + state guards all pass.
        /// </summary>
        Task<Result> HandlePaystackWebhookAsync(string rawBody, string? signatureHeader, CancellationToken cancellationToken = default);
    }
}
