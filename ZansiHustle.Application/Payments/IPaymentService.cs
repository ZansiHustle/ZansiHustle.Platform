using System;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Payments.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments
{
    public interface IPaymentService
    {
        /// <summary>
        /// Initializes a payment attempt for the given order owned by <paramref name="buyerUserId"/>.
        /// Reuses any existing non-terminal attempt for the order so retries are idempotent.
        /// Routes to the provider named on the request (defaults to Ozow when unspecified).
        /// </summary>
        Task<Result<InitializePaymentResponseDto>> InitializeAsync(
            Guid buyerUserId,
            InitializePaymentRequestDto request,
            CancellationToken cancellationToken = default);

        Task<Result<PaymentDto>> GetByIdAsync(Guid userId, Guid paymentId);

        /// <summary>
        /// Server-side verification against the provider by reference. Useful when the
        /// webhook is delayed or the mobile client returns to the app and wants a
        /// synchronous confirmation. Routes to the right provider based on the
        /// stored Payment.Provider.
        /// </summary>
        Task<Result<PaymentDto>> VerifyAsync(Guid userId, string reference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a Paystack webhook payload. Always completes (even on signature
        /// mismatch) by persisting an audit row; actionable events are applied only
        /// if signature + dedup key + state guards all pass.
        /// </summary>
        Task<Result> HandlePaystackWebhookAsync(string rawBody, string? signatureHeader, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes an Ozow TransactionNotificationResponse posted to NotifyUrl.
        /// Validates the response hash via <see cref="IOzowHashService"/>, dedupes
        /// via the (TransactionReference, Status) key, and applies the canonical
        /// Ozow status to the local Payment + Order. Always returns Result.Success
        /// so the controller can ack 200 to Ozow regardless.
        /// </summary>
        Task<Result> HandleOzowWebhookAsync(
            OzowTransactionNotification notification,
            string rawBody,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a Yoco webhook event delivered to /api/payments/webhook/yoco.
        /// Validates the Standard Webhooks signature via <see cref="IYocoSignatureService"/>,
        /// dedupes via the event id, and applies the Yoco status to the local
        /// Payment + Order. Always returns Result.Success so the controller can
        /// ack 200 to Yoco regardless.
        /// </summary>
        Task<Result> HandleYocoWebhookAsync(
            string rawBody,
            string? webhookId,
            string? webhookTimestamp,
            string? webhookSignature,
            CancellationToken cancellationToken = default);
    }
}
