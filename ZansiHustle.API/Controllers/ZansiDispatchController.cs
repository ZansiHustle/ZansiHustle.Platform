using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Application.ZansiDispatch.Dtos;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Shared.Enums.ZansiDispatch;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// ZansiDispatch — logistics control layer. Phase 1 surface: checkout
    /// delivery quotes + selection (buyer), and the command centre (admin/ops):
    /// shipments, actual-cost capture + reconciliation, and status.
    ///
    /// All endpoints require authentication. Quote endpoints act on the
    /// signed-in buyer; command-centre endpoints additionally require an
    /// internal role.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/zansidispatch")]
    public class ZansiDispatchController : BaseController
    {
        // Internal roles allowed to view the command centre.
        private const string CommandCentreReadRoles = "SuperAdmin,Admin,Partner,TeamManager";
        // Tighter set for write/reconciliation actions.
        private const string CommandCentreWriteRoles = "SuperAdmin,Admin";

        private readonly IZansiDispatchService _dispatch;
        private readonly ICurrentUserService _currentUser;
        private readonly IWebHostEnvironment _env;
        private readonly ZansiDispatchOptions _dispatchOptions;
        private readonly ILogger<ZansiDispatchController> _logger;

        public ZansiDispatchController(
            IZansiDispatchService dispatch,
            ICurrentUserService currentUser,
            IWebHostEnvironment env,
            IOptions<ZansiDispatchOptions> dispatchOptions,
            ILogger<ZansiDispatchController> logger)
        {
            _dispatch = dispatch;
            _currentUser = currentUser;
            _env = env;
            _dispatchOptions = dispatchOptions?.Value ?? new ZansiDispatchOptions();
            _logger = logger;
        }

        private bool TryGetUserId(out Guid userId)
        {
            userId = _currentUser.UserId ?? Guid.Empty;
            return userId != Guid.Empty;
        }

        private bool IsAdmin() => User.IsInRole("SuperAdmin") || User.IsInRole("Admin");

        // ── Checkout quoting (buyer) ────────────────────────────────────────

        /// <summary>Returns delivery options for checkout (Standard Delivery + optional Collection).</summary>
        [HttpPost("quotes")]
        public async Task<IActionResult> CreateQuote([FromBody] CreateQuoteRequestDto request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<QuoteDto>.Failure(ErrorCodes.Unauthorized, "Sign in to get delivery options."));

            // Client-supplied correlation id (UAT quote debug) — echoed into the
            // dispatch logs so app → API → provider can be matched. Optional.
            var correlationId = Request.Headers.TryGetValue("X-Correlation-Id", out var c)
                ? c.ToString()
                : null;

            return ToActionResult(await _dispatch.CreateQuoteAsync(userId, request, ct, correlationId));
        }

        /// <summary>Marks a delivery option selected before checkout/order creation.</summary>
        [HttpPost("quotes/{quoteId:guid}/select-option/{quoteOptionId:guid}")]
        public async Task<IActionResult> SelectOption(Guid quoteId, Guid quoteOptionId, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<QuoteDto>.Failure(ErrorCodes.Unauthorized, "Sign in to select a delivery option."));
            return ToActionResult(await _dispatch.SelectOptionAsync(userId, IsAdmin(), quoteId, quoteOptionId, ct));
        }

        // ── Command centre (admin/ops) ──────────────────────────────────────

        /// <summary>CEO/ops overview — delivery-fee totals, surplus/deficit/net, shipment-state counts.</summary>
        [HttpGet("command-centre/overview")]
        [Authorize(Roles = CommandCentreReadRoles)]
        public async Task<IActionResult> Overview([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
            => ToActionResult(await _dispatch.GetOverviewAsync(from, to, ct));

        // ── Checkout curation policy (DB-managed; ops-editable) ─────────────

        /// <summary>Returns the current checkout quote-curation policy for ops editing.</summary>
        [HttpGet("settings/curation")]
        [Authorize(Roles = CommandCentreReadRoles)]
        public async Task<IActionResult> GetCurationSettings(CancellationToken ct)
            => ToActionResult(await _dispatch.GetCheckoutCurationSettingsAsync(ct));

        /// <summary>Updates the checkout quote-curation policy (ops). No hard fee block here —
        /// only allowlist + outlier filtering + a soft high-fee warning threshold.</summary>
        [HttpPut("settings/curation")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> UpdateCurationSettings([FromBody] CheckoutCurationSettingsDto dto, CancellationToken ct)
            => ToActionResult(await _dispatch.UpdateCheckoutCurationSettingsAsync(dto ?? new CheckoutCurationSettingsDto(), ct));

        /// <summary>
        /// Lists shipments with SERVER-SIDE paging / sorting / filtering. Returns a
        /// paged envelope (items + total + page + pageSize). Defaults: createdAt desc
        /// (latest first), pageSize 25.
        /// </summary>
        [HttpGet("shipments")]
        [Authorize(Roles = CommandCentreReadRoles)]
        public async Task<IActionResult> Shipments([FromQuery] ShipmentQueryDto query, CancellationToken ct = default)
            => ToActionResult(await _dispatch.GetShipmentsAsync(query ?? new ShipmentQueryDto(), ct));

        /// <summary>Returns a single shipment's full detail.</summary>
        [HttpGet("shipments/{id:guid}")]
        [Authorize(Roles = CommandCentreReadRoles)]
        public async Task<IActionResult> Shipment(Guid id, CancellationToken ct)
            => ToActionResult(await _dispatch.GetShipmentAsync(id, ct));

        /// <summary>Captures the actual courier cost and reconciles surplus/deficit/net.</summary>
        [HttpPost("shipments/{id:guid}/actual-cost")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> CaptureActualCost(Guid id, [FromBody] CaptureActualCostRequestDto request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _dispatch.CaptureActualCostAsync(userId, id, request, ct));
        }

        /// <summary>Updates a shipment's delivery status.</summary>
        [HttpPost("shipments/{id:guid}/status")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateShipmentStatusRequestDto request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _dispatch.UpdateShipmentStatusAsync(userId, id, request, ct));
        }

        // ── Shipment lifecycle (provider-backed; admin/ops) ─────────────────

        /// <summary>Books a real courier shipment for an order from its selected quote option.</summary>
        [HttpPost("shipments/create-from-quote")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> CreateFromQuote([FromBody] CreateShipmentFromQuoteRequestDto request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _dispatch.CreateShipmentFromQuoteAsync(userId, request, ct));
        }

        /// <summary>
        /// Retries a failed / needs-attention courier booking from STORED data
        /// (no manual payload — addresses/parcel/contacts are resolved server-side).
        /// Idempotent; obeys every booking guard + the kill switch.
        /// </summary>
        [HttpPost("shipments/{id:guid}/retry-booking")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> RetryBooking(Guid id, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _dispatch.RetryBookingAsync(userId, id, ct));
        }

        /// <summary>Polls the courier, records events, and returns the tracking timeline.</summary>
        [HttpGet("shipments/{id:guid}/track")]
        [Authorize(Roles = CommandCentreReadRoles)]
        public async Task<IActionResult> Track(Guid id, CancellationToken ct)
            => ToActionResult(await _dispatch.TrackShipmentAsync(id, ct));

        /// <summary>Cancels a shipment with the courier (when booked) and internally.</summary>
        [HttpPost("shipments/{id:guid}/cancel")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelShipmentRequestDto? request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _dispatch.CancelShipmentAsync(userId, id, request, ct));
        }

        /// <summary>Returns the signed label/waybill URL (admin/ops only).</summary>
        [HttpGet("shipments/{id:guid}/label")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> Label(Guid id, CancellationToken ct)
            => ToActionResult(await _dispatch.GetShipmentLabelAsync(id, ct));

        // ── Post-acceptance lifecycle (admin/ops) ───────────────────────────

        /// <summary>Refresh live courier status from the provider + record the action.</summary>
        [HttpPost("shipments/{id:guid}/refresh-status")]
        [Authorize(Roles = CommandCentreReadRoles)]
        public async Task<IActionResult> RefreshStatus(Guid id, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _dispatch.RefreshStatusAsync(userId, id, ct));
        }

        /// <summary>Status-based provider cancellation (refresh → cancel / block / needs-attention).</summary>
        [HttpPost("shipments/{id:guid}/cancel-provider")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> CancelProvider(Guid id, [FromBody] CancelShipmentRequestDto? request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _dispatch.CancelProviderAsync(userId, id, request, ct));
        }

        /// <summary>Reschedule pickup (provider call if supported, else an ops task — never faked).</summary>
        [HttpPost("shipments/{id:guid}/reschedule-pickup")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> ReschedulePickup(Guid id, [FromBody] ReschedulePickupRequestDto request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            return ToActionResult(await _dispatch.ReschedulePickupAsync(userId, ZansiDispatchActor.Admin, id, request, ct));
        }

        /// <summary>Submit a delivery-date-change request on a shipment (ops, on the customer's behalf).</summary>
        [HttpPost("shipments/{id:guid}/request-delivery-change")]
        [Authorize(Roles = CommandCentreWriteRoles)]
        public async Task<IActionResult> RequestDeliveryChange(Guid id, [FromBody] RequestDeliveryChangeRequestDto request, CancellationToken ct)
        {
            if (!TryGetUserId(out var userId))
                return ToActionResult(Result<ShipmentDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));
            // Ops submits on behalf — the service still records the action against the shipment.
            return ToActionResult(await _dispatch.RequestDeliveryChangeAsync(userId, id, request, ct));
        }

        /// <summary>The shipment "Activity / Actions" audit timeline.</summary>
        [HttpGet("shipments/{id:guid}/actions")]
        [Authorize(Roles = CommandCentreReadRoles)]
        public async Task<IActionResult> Actions(Guid id, CancellationToken ct)
            => ToActionResult(await _dispatch.GetShipmentActionsAsync(id, ct));

        // ── Webhook foundation (courier → us; anonymous + optional secret) ──

        /// <summary>
        /// Inbound Courier Guy / Shiplogic webhook. Anonymous (the courier calls
        /// it); an optional shared secret is verified inside the service. Logs the
        /// raw event, matches a shipment, records tracking events, and acks 200
        /// quickly so the courier doesn't retry-storm.
        /// </summary>
        [HttpPost("webhooks/courier-guy")]
        [AllowAnonymous]
        public async Task<IActionResult> CourierGuyWebhook(CancellationToken ct)
        {
            // Fail-CLOSED outside Development: a provider webhook is unauthenticated
            // (no JWT), so its only protection is the shared secret. If the secret is
            // not configured we must reject in UAT/Staging/Production rather than
            // silently accept unauthenticated calls. In Development a missing secret
            // is allowed but loudly logged (so local testing isn't blocked). The
            // secret value is never logged.
            var secretConfigured = !string.IsNullOrWhiteSpace(_dispatchOptions.CourierGuy.WebhookSecret);
            if (!secretConfigured)
            {
                if (!_env.IsDevelopment())
                {
                    _logger.LogWarning(
                        "CourierGuy webhook REJECTED (401): ZansiDispatch:CourierGuy:WebhookSecret is not configured in {Environment}. " +
                        "Set the secret to enable provider webhooks.", _env.EnvironmentName);
                    return ToActionResult(Result<WebhookAckDto>.Failure(
                        ErrorCodes.Unauthorized, "Webhook authentication is not configured."));
                }

                _logger.LogWarning(
                    "CourierGuy webhook accepted WITHOUT a configured secret because the environment is Development. " +
                    "This is fail-open ONLY in Development; set ZansiDispatch:CourierGuy:WebhookSecret before UAT/prod.");
            }

            string rawBody;
            using (var reader = new System.IO.StreamReader(Request.Body))
                rawBody = await reader.ReadToEndAsync(ct);

            var authHeader = Request.Headers.TryGetValue("Authorization", out var a)
                ? a.ToString()
                : (Request.Headers.TryGetValue("X-Webhook-Secret", out var x) ? x.ToString() : null);

            // When a secret IS configured, the service performs the constant
            // Authorization / X-Webhook-Secret match and returns 401 on mismatch.
            return ToActionResult(await _dispatch.HandleCourierWebhookAsync(rawBody, authHeader, ct));
        }
    }
}
