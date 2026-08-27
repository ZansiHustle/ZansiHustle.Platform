using System;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Payments.External.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Payments.External
{
    public interface IExternalShopPaymentService
    {
        /// <summary>
        /// POST /external-payments/sessions. Validates the shared secret and
        /// shop registration, validates return/callback URLs against the
        /// shop's allow-list, and is idempotent on (ShopCode, ExternalOrderId).
        /// </summary>
        Task<Result<CreateExternalPaymentSessionResponseDto>> CreateSessionAsync(
            CreateExternalPaymentSessionRequestDto? request,
            string? providedSharedSecret,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// GET /external-payments/sessions/{sessionId}. Ownership-checked
        /// against the session's own ShopCode — a shop can never see another
        /// shop's session. Opportunistically retries callback delivery if the
        /// session is terminal and undelivered.
        /// </summary>
        Task<Result<ExternalPaymentSessionStatusResponseDto>> GetStatusAsync(
            Guid sessionId,
            string? providedSharedSecret,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the shop-facing redirect target for an Ozow browser
        /// return (success/cancel/error — the label itself is never trusted).
        /// Performs a best-effort reconciliation via Ozow's own transaction
        /// lookup, never from the unverified query string. Returns null when
        /// no session can be resolved from the reference.
        /// </summary>
        Task<Uri?> ResolveBrowserReturnAsync(string? transactionReference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Authoritative settlement path — Ozow's NotifyUrl webhook for
        /// external-payment sessions. Hash-verified and idempotent; always
        /// safe to call repeatedly.
        /// </summary>
        Task HandleOzowWebhookAsync(OzowTransactionNotification notification, string rawBody, CancellationToken cancellationToken = default);
    }
}
