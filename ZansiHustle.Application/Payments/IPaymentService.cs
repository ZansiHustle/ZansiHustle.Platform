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
        /// DEV/UAT-ONLY: simulate a successful gateway payment for an order the
        /// caller owns, WITHOUT contacting Ozow. Settles the order through the
        /// SAME internal paid-transition path as a real provider success
        /// (PaymentStatus → Paid, product order → AwaitingSellerAcceptance,
        /// seller notified, wallet split honoured) — and, critically, does NOT
        /// book dispatch (dispatch still waits for explicit seller acceptance).
        /// Refuses with NOT_FOUND when <c>Payments:MockCheckoutEnabled</c> is
        /// false. Idempotent: a second call on an already-paid order is a no-op.
        /// </summary>
        Task<Result<PaymentDto>> MockOrderSuccessAsync(
            Guid userId,
            MockOrderSuccessRequestDto request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Server-side verification against the provider by reference. Useful when the
        /// webhook is delayed or the mobile client returns to the app and wants a
        /// synchronous confirmation. Routes to the right provider based on the
        /// stored Payment.Provider.
        /// </summary>
        Task<Result<PaymentDto>> VerifyAsync(Guid userId, string reference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Buyer-initiated cancel of a non-terminal payment (e.g. they backed out of
        /// the gateway browser). Marks the payment Cancelled and runs the failure
        /// path — which REVERSES any wallet hold so split-payment funds are restored.
        /// Idempotent + a no-op on an already-settled (Succeeded) payment.
        /// </summary>
        Task<Result<PaymentDto>> CancelAsync(Guid userId, string reference, CancellationToken cancellationToken = default);

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
