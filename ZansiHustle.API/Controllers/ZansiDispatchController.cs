using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Application.ZansiDispatch.Dtos;
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

        public ZansiDispatchController(IZansiDispatchService dispatch, ICurrentUserService currentUser)
        {
            _dispatch = dispatch;
            _currentUser = currentUser;
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
            return ToActionResult(await _dispatch.CreateQuoteAsync(userId, request, ct));
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

        /// <summary>Lists shipments, optionally filtered by status.</summary>
        [HttpGet("shipments")]
        [Authorize(Roles = CommandCentreReadRoles)]
        public async Task<IActionResult> Shipments(
            [FromQuery] ZansiDispatchShipmentStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 30,
            CancellationToken ct = default)
            => ToActionResult(await _dispatch.GetShipmentsAsync(status, page, pageSize, ct));

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
            string rawBody;
            using (var reader = new System.IO.StreamReader(Request.Body))
                rawBody = await reader.ReadToEndAsync(ct);

            var authHeader = Request.Headers.TryGetValue("Authorization", out var a)
                ? a.ToString()
                : (Request.Headers.TryGetValue("X-Webhook-Secret", out var x) ? x.ToString() : null);

            return ToActionResult(await _dispatch.HandleCourierWebhookAsync(rawBody, authHeader, ct));
        }
    }
}
