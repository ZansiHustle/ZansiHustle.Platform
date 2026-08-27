using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Payments.External;
using ZansiHustle.Application.Payments.External.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Server-to-server External Shop Payments API. NOT a public browser
    /// checkout API — every non-webhook, non-return endpoint requires the
    /// X-ZansiHustle-Shared-Secret header issued to a registered shop.
    ///
    /// Deliberately routed WITHOUT the "api/" prefix — ZansiTech's frozen
    /// contract expects "{BaseUrl}/external-payments/sessions" with
    /// BaseUrl = https://api.zansihustle.com. This codebase has no global
    /// "/api" prefix mechanism (every controller opts in via its own [Route]
    /// literal), so this simply opts out. One side effect used deliberately:
    /// AppVersionGateMiddleware only inspects paths starting with "/api", so
    /// this whole controller is automatically exempt from the mobile
    /// hard-update gate — correct, since none of these callers are the
    /// ZansiHustle mobile app.
    ///
    /// Response shape is intentionally NOT wrapped in the {success,message,data}
    /// envelope BaseController uses for ZansiHustle's own clients — ZansiTech's
    /// contract is frozen to the raw {sessionId,redirectUrl} / {status,failureReason}
    /// shapes, so this controller inherits ControllerBase directly.
    /// </summary>
    [ApiController]
    [Route("external-payments")]
    public class ExternalPaymentsController : ControllerBase
    {
        private const string SharedSecretHeader = "X-ZansiHustle-Shared-Secret";

        private readonly IExternalShopPaymentService _service;
        private readonly ILogger<ExternalPaymentsController> _logger;

        public ExternalPaymentsController(IExternalShopPaymentService service, ILogger<ExternalPaymentsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>POST /external-payments/sessions — create (or idempotently reuse) a payment session.</summary>
        [HttpPost("sessions")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CreateExternalPaymentSessionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSession([FromBody] CreateExternalPaymentSessionRequestDto? request, CancellationToken cancellationToken)
        {
            var secret = ReadSharedSecret();
            var result = await _service.CreateSessionAsync(request, secret, cancellationToken);
            return FromResult(result);
        }

        /// <summary>GET /external-payments/sessions/{sessionId} — authoritative status; ownership-checked against the shared secret.</summary>
        [HttpGet("sessions/{sessionId:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExternalPaymentSessionStatusResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSessionStatus(Guid sessionId, CancellationToken cancellationToken)
        {
            var secret = ReadSharedSecret();
            var result = await _service.GetStatusAsync(sessionId, secret, cancellationToken);
            return FromResult(result);
        }

        // ─── Ozow authoritative webhook (dedicated NotifyUrl for external sessions) ──

        /// <summary>
        /// Dedicated Ozow NotifyUrl for external-payment sessions — set as this
        /// session's own NotifyUrl at PostPaymentRequest time, so it never
        /// touches the internal marketplace webhook at api/payments/webhook/ozow.
        /// Hash-verified, idempotent, always 200 OK so Ozow stops retrying.
        /// </summary>
        [HttpPost("ozow/webhook")]
        [AllowAnonymous]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> OzowWebhook(CancellationToken cancellationToken)
        {
            Request.EnableBuffering();
            Request.Body.Position = 0;

            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync(cancellationToken);
            Request.Body.Position = 0;

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

            await _service.HandleOzowWebhookAsync(notification, rawBody, cancellationToken);
            return Ok(new { received = true });
        }

        // ─── Ozow browser-return endpoints (dedicated Success/Cancel/ErrorUrl) ──
        // Pure user-redirect targets. Never authoritative for payment success —
        // only the webhook above (or the best-effort Ozow lookup this triggers)
        // may transition a session's state. Always redirect back to the shop's
        // OWN validated ReturnUrl; never accept a redirect target from the
        // returning browser itself.

        [HttpGet("ozow/return/success")]
        [HttpPost("ozow/return/success")]
        [AllowAnonymous]
        public Task<IActionResult> ReturnSuccess(CancellationToken cancellationToken) => HandleReturnAsync(cancellationToken);

        [HttpGet("ozow/return/cancel")]
        [HttpPost("ozow/return/cancel")]
        [AllowAnonymous]
        public Task<IActionResult> ReturnCancel(CancellationToken cancellationToken) => HandleReturnAsync(cancellationToken);

        [HttpGet("ozow/return/error")]
        [HttpPost("ozow/return/error")]
        [AllowAnonymous]
        public Task<IActionResult> ReturnError(CancellationToken cancellationToken) => HandleReturnAsync(cancellationToken);

        private async Task<IActionResult> HandleReturnAsync(CancellationToken cancellationToken)
        {
            var txRef = await ReadTransactionReferenceAsync(cancellationToken);

            _logger.LogInformation("[ExternalPayments][Return] ref={Ref}", txRef ?? "<none>");

            var redirect = await _service.ResolveBrowserReturnAsync(txRef, cancellationToken);
            if (redirect is null)
            {
                return Content("Payment response received. Please return to the shop to verify payment.", "text/plain; charset=utf-8");
            }

            return Redirect(redirect.ToString());
        }

        private async Task<string?> ReadTransactionReferenceAsync(CancellationToken cancellationToken)
        {
            var txRef = NullIfBlank(Request.Query["TransactionReference"].ToString());

            if (txRef is null && Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                txRef = NullIfBlank(form["TransactionReference"].ToString());
            }

            return txRef;
        }

        private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

        private string? ReadSharedSecret() =>
            Request.Headers.TryGetValue(SharedSecretHeader, out var value) ? value.ToString() : null;

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

        // ─── Result → raw JSON mapping (NOT the {success,message,data} envelope) ──

        private IActionResult FromResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Data);

            return StatusCode(MapStatus(result.Code), new { error = result.Code, message = result.Message });
        }

        private static int MapStatus(string code) => code switch
        {
            ErrorCodes.BadRequest => StatusCodes.Status400BadRequest,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            ErrorCodes.PaymentInitFailed => StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.PaymentProviderUnavailable => StatusCodes.Status422UnprocessableEntity,
            ErrorCodes.ProviderNotConfigured => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
