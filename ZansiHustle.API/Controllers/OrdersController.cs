using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Application.Orders;
using ZansiHustle.Application.Orders.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Buyer + seller order endpoints. All routes require an authenticated user;
    /// ownership rules are enforced in <see cref="OrderService"/> via the JWT user id.
    /// </summary>
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : BaseController
    {
        private readonly IOrderService _orderService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            ICurrentUserService currentUserService,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>Places a new order as the current authenticated buyer.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequestDto request)
        {
            // Anchor log so anyone needing the orderId for Swagger testing
            // can grep stdout for `[Orders][Create] OK orderId=…` and copy
            // it straight into POST /api/Payments/initialize. Pair this
            // with the Service-level success log + elapsed ms here.
            var sw = Stopwatch.StartNew();

            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<OrderDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _orderService.CreateAsync(userId.Value, request);

            _logger.LogInformation(
                "[Orders][Create] settled success={Success} code={Code} userId={UserId} orderId={OrderId} orderCode={OrderCode} elapsedMs={Elapsed}",
                result.IsSuccess,
                result.IsSuccess ? "OK" : result.Code,
                userId.Value,
                result.IsSuccess ? result.Data?.Id : (Guid?)null,
                result.IsSuccess ? result.Data?.Code : null,
                sw.ElapsedMilliseconds);

            return ToActionResult(result);
        }

        /// <summary>Lists orders placed by the current authenticated user (buyer view).</summary>
        [HttpGet("mine")]
        [ProducesResponseType(typeof(Result<List<OrderListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine()
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<List<OrderListItemDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _orderService.GetMineAsync(userId.Value);
            return ToActionResult(result);
        }

        /// <summary>Lists orders against shops owned by the current user (seller view).</summary>
        [HttpGet("seller")]
        [ProducesResponseType(typeof(Result<List<OrderListItemDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSellerOrders()
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<List<OrderListItemDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _orderService.GetForSellerAsync(userId.Value);
            return ToActionResult(result);
        }

        /// <summary>Returns the order detail; the caller must be the buyer or the merchant owner.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<OrderDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _orderService.GetByIdAsync(userId.Value, id);
            return ToActionResult(result);
        }

        /// <summary>
        /// Updates the order status. Buyers can only cancel their own pending orders;
        /// sellers transition through Pending → Confirmed → InProgress → Completed
        /// (with Cancelled available at any non-terminal step).
        /// </summary>
        [HttpPut("{id:guid}/status")]
        [ProducesResponseType(typeof(Result<OrderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequestDto request)
        {
            var userId = _currentUserService.UserId;

            if (!userId.HasValue)
                return ToActionResult(Result<OrderDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found in token."));

            var result = await _orderService.UpdateStatusAsync(userId.Value, id, request);
            return ToActionResult(result);
        }
    }
}
