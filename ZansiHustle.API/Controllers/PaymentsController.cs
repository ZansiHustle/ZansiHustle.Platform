using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Payments;
using ZansiHustle.Application.Payments.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Buyer payment endpoints + Paystack webhook receiver.
    /// </summary>
    [Route("api/[controller]")]
    public class PaymentsController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly ICurrentUserService _currentUserService;

        public PaymentsController(IPaymentService paymentService, ICurrentUserService currentUserService)
        {
            _paymentService = paymentService;
            _currentUserService = currentUserService;
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
    }
}
