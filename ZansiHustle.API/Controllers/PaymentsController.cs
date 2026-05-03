using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Payments;
using ZansiHustle.Application.Payments.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Buyer payment endpoints + Paystack/Ozow webhook receivers + Ozow
    /// redirect-return endpoints.
    /// </summary>
    [Route("api/[controller]")]
    public class PaymentsController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly ICurrentUserService _currentUserService;
        private readonly OzowSettings _ozowSettings;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPaymentService paymentService,
            ICurrentUserService currentUserService,
            IOptions<OzowSettings> ozowSettings,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _currentUserService = currentUserService;
            _ozowSettings = ozowSettings.Value ?? new OzowSettings();
            _logger = logger;
        }

        /// <summary>Initializes a new payment attempt against the caller's order.</summary>
        [HttpPost("initialize")]
        [Authorize]
        [ProducesResponseType(typeof(Result<InitializePaymentResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Initialize([FromBody] InitializePaymentRequestDto request, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<InitializePaymentResponseDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _paymentService.InitializeAsync(userId.Value, request, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>Returns a single payment owned by the caller.</summary>
        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<PaymentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<PaymentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _paymentService.GetByIdAsync(userId.Value, id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Server-side verify against the provider by reference. Safe to call
        /// repeatedly; reconciles state idempotently.
        /// </summary>
        [HttpPost("verify/{reference}")]
        [Authorize]
        [ProducesResponseType(typeof(Result<PaymentDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Verify(string reference, CancellationToken cancellationToken)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<PaymentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _paymentService.VerifyAsync(userId.Value, reference, cancellationToken);
            return ToActionResult(result);
        }

        /// <summary>
        /// Paystack webhook endpoint. Publicly reachable but signature-verified
        /// and idempotent — always returns 200 so Paystack stops retrying.
        /// </summary>
        [HttpPost("webhook/paystack")]
        [AllowAnonymous]
        public async Task<IActionResult> PaystackWebhook(CancellationToken cancellationToken)
        {
            // Read the raw body — HMAC verification runs over the exact bytes Paystack sent.
            Request.EnableBuffering();
            Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync(cancellationToken);
            Request.Body.Position = 0;

            var signature = Request.Headers.TryGetValue("x-paystack-signature", out var sig)
                ? sig.ToString()
                : null;

            // Always ack with 200 — status is purely for our own ops, never for the provider.
            await _paymentService.HandlePaystackWebhookAsync(rawBody, signature, cancellationToken);
            return Ok(new { received = true });
        }

        /// <summary>
        /// Ozow NotifyUrl receiver. Ozow POSTs the TransactionNotificationResponse
        /// as <c>application/x-www-form-urlencoded</c>. Hash-verified and idempotent —
        /// always returns 200 OK so Ozow stops retrying.
        /// </summary>
        [HttpPost("webhook/ozow")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> OzowWebhook(CancellationToken cancellationToken)
        {
            // Capture the raw body for the audit log before we touch the form parser.
            Request.EnableBuffering();
            Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync(cancellationToken);
            Request.Body.Position = 0;

            // Bind from the form. Ozow uses Title-cased field names.
            var form = await Request.ReadFormAsync(cancellationToken);

            var notification = new OzowTransactionNotification
            {
                SiteCode = form["SiteCode"],
                TransactionId = form["TransactionId"],
                TransactionReference = form["TransactionReference"],
                Amount = ParseDecimal(form["Amount"]),
                Status = form["Status"],
                Optional1 = form["Optional1"],
                Optional2 = form["Optional2"],
                Optional3 = form["Optional3"],
                Optional4 = form["Optional4"],
                Optional5 = form["Optional5"],
                CurrencyCode = form["CurrencyCode"],
                IsTest = ParseBool(form["IsTest"]),
                StatusMessage = form["StatusMessage"],
                Hash = form["Hash"],
                SubStatus = form["SubStatus"],
                MaskedAccountNumber = form["MaskedAccountNumber"],
                BankName = form["BankName"],
                SmartIndicators = form["SmartIndicators"],
            };

            await _paymentService.HandleOzowWebhookAsync(notification, rawBody, cancellationToken);
            return Ok(new { received = true });
        }

        private static decimal ParseDecimal(Microsoft.Extensions.Primitives.StringValues raw)
        {
            var s = raw.ToString();
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;
        }

        private static bool ParseBool(Microsoft.Extensions.Primitives.StringValues raw)
        {
            var s = raw.ToString();
            return bool.TryParse(s, out var v) && v;
        }

        // ─── Ozow user-redirect return endpoints ─────────────────────────────
        // Ozow sends the buyer back to one of these three URLs after the bank
        // flow. They are PURE user-redirect targets:
        //   • anonymous (the buyer's session may not exist on the device)
        //   • never mark a payment as paid — Ozow itself says the success
        //     redirect is not authoritative; only the webhook + verify call
        //     are trusted to mutate state
        //   • capture context (TransactionReference, TransactionId, Status)
        //     for the log so we can correlate with the real webhook event
        //   • either deep-link the buyer back into the mobile app (when
        //     Ozow:AppReturnDeepLink is configured) or render a friendly
        //     fallback message asking them to return to the app.
        // The mobile app must still call POST /api/payments/verify/{reference}
        // after returning to confirm the actual outcome.

        /// <summary>Ozow Success redirect target. Does NOT mark the payment paid.</summary>
        [HttpGet("ozow/return/success")]
        [HttpPost("ozow/return/success")]
        [AllowAnonymous]
        public Task<IActionResult> OzowReturnSuccess(CancellationToken cancellationToken)
            => HandleOzowReturnAsync("success", cancellationToken);

        /// <summary>Ozow Cancel redirect target. Does NOT mutate payment state.</summary>
        [HttpGet("ozow/return/cancel")]
        [HttpPost("ozow/return/cancel")]
        [AllowAnonymous]
        public Task<IActionResult> OzowReturnCancel(CancellationToken cancellationToken)
            => HandleOzowReturnAsync("cancel", cancellationToken);

        /// <summary>Ozow Error redirect target. Does NOT mutate payment state.</summary>
        [HttpGet("ozow/return/error")]
        [HttpPost("ozow/return/error")]
        [AllowAnonymous]
        public Task<IActionResult> OzowReturnError(CancellationToken cancellationToken)
            => HandleOzowReturnAsync("error", cancellationToken);

        private async Task<IActionResult> HandleOzowReturnAsync(string result, CancellationToken cancellationToken)
        {
            var (txRef, txId, status) = await ReadOzowReturnContextAsync(cancellationToken);

            // Log only the safe correlation fields. Hash, ApiKey, PrivateKey
            // never appear here — see OzowClient + OzowHashService for the
            // wider non-leak guarantee.
            _logger.LogInformation(
                "[Ozow][Return] result={Result} ref={Ref} txId={TxId} status={Status}",
                result, txRef ?? "<none>", txId ?? "<none>", status ?? "<none>");

            // If a deep link is configured, send the buyer straight back into
            // the app with enough context for the app to call the verify
            // endpoint. Otherwise serve a plain fallback so the user isn't
            // staring at a 404 in their bank's web view.
            var deepLink = _ozowSettings.AppReturnDeepLink;
            if (!string.IsNullOrWhiteSpace(deepLink))
            {
                var sep = deepLink.Contains('?') ? '&' : '?';
                var qs = $"result={Uri.EscapeDataString(result)}";
                if (!string.IsNullOrWhiteSpace(txRef))
                    qs += $"&reference={Uri.EscapeDataString(txRef)}";
                if (!string.IsNullOrWhiteSpace(status))
                    qs += $"&status={Uri.EscapeDataString(status)}";

                // permanent: false — these are not cacheable browser redirects.
                return Redirect($"{deepLink}{sep}{qs}");
            }

            return Content(
                "Payment response received. Please return to the app to verify payment.",
                "text/plain; charset=utf-8");
        }

        private async Task<(string? Ref, string? TxId, string? Status)> ReadOzowReturnContextAsync(CancellationToken cancellationToken)
        {
            // Ozow most commonly returns the buyer via a GET with query
            // parameters, but the docs allow form-POST too. Read query first,
            // then fall back to form fields if the verb is POST.
            string? txRef = NullIfBlank(Request.Query["TransactionReference"].ToString());
            string? txId = NullIfBlank(Request.Query["TransactionId"].ToString());
            string? status = NullIfBlank(Request.Query["Status"].ToString());

            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                txRef ??= NullIfBlank(form["TransactionReference"].ToString());
                txId ??= NullIfBlank(form["TransactionId"].ToString());
                status ??= NullIfBlank(form["Status"].ToString());
            }

            return (txRef, txId, status);
        }

        private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
