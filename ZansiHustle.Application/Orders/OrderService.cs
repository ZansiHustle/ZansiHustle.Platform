using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Orders.Dtos;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IListingRepository _listingRepository;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IListingRepository listingRepository, UserManager<User> userManager, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _listingRepository = listingRepository;
            _userManager = userManager;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Result<OrderDto>> CreateAsync(Guid buyerUserId, CreateOrderRequestDto request)
        {
            try
            {
                if (request is null || request.Items is null || request.Items.Count == 0)
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "At least one order item is required.");

                // Collapse duplicate listing ids into a single line item so the
                // final order is clean regardless of how the client structured
                // the payload.
                var grouped = request.Items
                    .Where(i => i.Quantity > 0)
                    .GroupBy(i => i.ListingId)
                    .Select(g => new { ListingId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                    .ToList();

                if (grouped.Count == 0)
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "At least one order item with a positive quantity is required.");

                // Load all listings up-front so we can validate merchant consistency
                // and snapshot prices server-side.
                var listings = new List<Domain.Listings.Listing>();
                foreach (var line in grouped)
                {
                    var listing = await _listingRepository.GetByIdAsync(line.ListingId);

                    if (listing is null)
                        return Result<OrderDto>.Failure(ErrorCodes.NotFound, $"Listing {line.ListingId} not found.");

                    if (listing.Status != ZansiHustle.Shared.Enums.Listings.ListingStatus.Active)
                        return Result<OrderDto>.Failure(ErrorCodes.BadRequest, $"Listing '{listing.Title}' is not available for purchase.");

                    listings.Add(listing);
                }

                var merchantIds = listings.Select(l => l.MerchantId).Distinct().ToList();
                if (merchantIds.Count > 1)
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "All items in an order must belong to the same shop. Please place separate orders for items from different shops.");

                var merchantId = merchantIds[0];

                // Buyer should not be the shop owner (can't order from yourself).
                var firstListing = listings[0];
                if (firstListing.Merchant?.OwnerUserId == buyerUserId)
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "You cannot place an order against your own shop.");

                var buyer = await _userManager.FindByIdAsync(buyerUserId.ToString());

                var currency = firstListing.Currency ?? "ZAR";

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    Code = GenerateCode(),
                    BuyerUserId = buyerUserId,
                    BuyerName = buyer is null ? null : $"{buyer.FirstName} {buyer.LastName}".Trim(),
                    BuyerEmail = buyer?.Email,
                    BuyerPhone = buyer?.PhoneNumber,
                    MerchantId = merchantId,
                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.Pending,
                    Currency = currency,
                    DeliveryAddress = request.DeliveryAddress?.Trim(),
                    Notes = request.Notes?.Trim(),
                    CreatedAtUtc = DateTime.UtcNow
                };

                decimal subtotal = 0m;
                foreach (var line in grouped)
                {
                    var listing = listings.First(l => l.Id == line.ListingId);

                    var unitPrice = listing.Price;
                    var lineTotal = unitPrice * line.Quantity;
                    subtotal += lineTotal;

                    order.Items.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ListingId = listing.Id,
                        ListingType = listing.Type,
                        TitleSnapshot = listing.Title,
                        ImageSnapshot = listing.Images.Count > 0 ? listing.Images[0] : null,
                        UnitPrice = unitPrice,
                        Quantity = line.Quantity,
                        LineTotal = lineTotal,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                order.Subtotal = subtotal;
                order.Total = subtotal; // v1: no tax/delivery charges computed server-side.

                await _orderRepository.AddAsync(order);
                var saved = await _orderRepository.SaveChangesAsync();

                if (!saved)
                    return Result<OrderDto>.Failure(ErrorCodes.Exception, "Failed to create order.");

                var reloaded = await _orderRepository.GetByIdAsync(order.Id);
                return Result<OrderDto>.Success(MapToDto(reloaded ?? order), "Order placed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order create failed for buyer {BuyerId}.", buyerUserId);
                return Result<OrderDto>.Failure(ErrorCodes.Exception, $"Failed to place order. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<OrderListItemDto>>> GetMineAsync(Guid buyerUserId)
        {
            try
            {
                var orders = await _orderRepository.GetByBuyerAsync(buyerUserId);
                var data = orders.Select(MapToListItem).ToList();

                return Result<List<OrderListItemDto>>.Success(data, "Your orders retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve buyer orders for {BuyerId}.", buyerUserId);
                return Result<List<OrderListItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve your orders. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<List<OrderListItemDto>>> GetForSellerAsync(Guid sellerUserId)
        {
            try
            {
                var orders = await _orderRepository.GetBySellerUserAsync(sellerUserId);
                var data = orders.Select(MapToListItem).ToList();

                return Result<List<OrderListItemDto>>.Success(data, "Seller orders retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve seller orders for {SellerId}.", sellerUserId);
                return Result<List<OrderListItemDto>>.Failure(ErrorCodes.Exception, $"Failed to retrieve seller orders. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<OrderDto>> GetByIdAsync(Guid userId, Guid orderId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order is null)
                    return Result<OrderDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                if (!IsBuyerOrSeller(order, userId))
                    return Result<OrderDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to view this order.");

                return Result<OrderDto>.Success(MapToDto(order), "Order retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve order {OrderId} for user {UserId}.", orderId, userId);
                return Result<OrderDto>.Failure(ErrorCodes.Exception, $"Failed to retrieve order. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<OrderDto>> UpdateStatusAsync(Guid userId, Guid orderId, UpdateOrderStatusRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order is null)
                    return Result<OrderDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                var isBuyer = order.BuyerUserId == userId;
                var isSeller = order.Merchant?.OwnerUserId == userId;

                if (!isBuyer && !isSeller)
                    return Result<OrderDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to update this order.");

                var transitionCheck = ValidateTransition(order.Status, request.Status, isBuyer, isSeller);
                if (!transitionCheck.IsSuccess)
                    return Result<OrderDto>.Failure(transitionCheck.Code, transitionCheck.Message);

                var now = DateTime.UtcNow;
                order.Status = request.Status;
                order.UpdatedAtUtc = now;

                switch (request.Status)
                {
                    case OrderStatus.Confirmed:
                        order.ConfirmedAtUtc = now;
                        break;
                    case OrderStatus.Completed:
                        order.CompletedAtUtc = now;
                        break;
                    case OrderStatus.Cancelled:
                        order.CancelledAtUtc = now;
                        order.CancellationReason = request.CancellationReason?.Trim();
                        break;
                }

                _orderRepository.Update(order);
                var saved = await _orderRepository.SaveChangesAsync();

                if (!saved)
                    return Result<OrderDto>.Failure(ErrorCodes.Exception, "Failed to update order.");

                var reloaded = await _orderRepository.GetByIdAsync(order.Id);
                return Result<OrderDto>.Success(MapToDto(reloaded ?? order), "Order updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order {OrderId} for user {UserId}.", orderId, userId);
                return Result<OrderDto>.Failure(ErrorCodes.Exception, $"Failed to update order. {ex.Message}");
            }
        }

        private static bool IsBuyerOrSeller(Order order, Guid userId)
        {
            if (order.BuyerUserId == userId) return true;
            if (order.Merchant?.OwnerUserId == userId) return true;

            return false;
        }

        private static Result ValidateTransition(OrderStatus current, OrderStatus next, bool isBuyer, bool isSeller)
        {
            if (current == OrderStatus.Completed)
                return Result.Failure(ErrorCodes.BadRequest, "Completed orders cannot be changed.");

            if (current == OrderStatus.Cancelled)
                return Result.Failure(ErrorCodes.BadRequest, "Cancelled orders cannot be changed.");

            if (current == next)
                return Result.Failure(ErrorCodes.BadRequest, "Order is already in that status.");

            // Buyer can only cancel their own pending order.
            if (isBuyer && !isSeller)
            {
                if (next == OrderStatus.Cancelled && current == OrderStatus.Pending)
                    return Result.Success();

                return Result.Failure(ErrorCodes.Forbidden, "Buyers can only cancel pending orders.");
            }

            // Seller transitions.
            if (isSeller)
            {
                switch (current)
                {
                    case OrderStatus.Pending:
                        if (next == OrderStatus.Confirmed || next == OrderStatus.Cancelled)
                            return Result.Success();
                        break;

                    case OrderStatus.Confirmed:
                        if (next == OrderStatus.InProgress || next == OrderStatus.Cancelled)
                            return Result.Success();
                        break;

                    case OrderStatus.InProgress:
                        if (next == OrderStatus.Completed || next == OrderStatus.Cancelled)
                            return Result.Success();
                        break;
                }

                return Result.Failure(ErrorCodes.BadRequest, $"Cannot transition from {current} to {next}.");
            }

            return Result.Failure(ErrorCodes.Forbidden, "You do not have permission to update this order.");
        }

        private static string GenerateCode()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                Code = order.Code,
                BuyerUserId = order.BuyerUserId,
                BuyerName = order.BuyerName,
                BuyerEmail = order.BuyerEmail,
                BuyerPhone = order.BuyerPhone,
                MerchantId = order.MerchantId,
                MerchantName = order.Merchant?.Name,
                MerchantSlug = order.Merchant?.Slug,
                MerchantLogoUrl = order.Merchant?.LogoUrl,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                Subtotal = order.Subtotal,
                Total = order.Total,
                Currency = order.Currency,
                DeliveryAddress = order.DeliveryAddress,
                Notes = order.Notes,
                CancellationReason = order.CancellationReason,
                CreatedAtUtc = order.CreatedAtUtc,
                UpdatedAtUtc = order.UpdatedAtUtc,
                ConfirmedAtUtc = order.ConfirmedAtUtc,
                CompletedAtUtc = order.CompletedAtUtc,
                CancelledAtUtc = order.CancelledAtUtc,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ListingId = i.ListingId,
                    ListingType = i.ListingType,
                    Title = i.TitleSnapshot,
                    ImageUrl = i.ImageSnapshot,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal
                }).ToList()
            };
        }

        private static OrderListItemDto MapToListItem(Order order)
        {
            var first = order.Items.FirstOrDefault();
            return new OrderListItemDto
            {
                Id = order.Id,
                Code = order.Code,
                BuyerUserId = order.BuyerUserId,
                BuyerName = order.BuyerName,
                MerchantId = order.MerchantId,
                MerchantName = order.Merchant?.Name,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                Total = order.Total,
                Currency = order.Currency,
                ItemCount = order.Items.Sum(i => i.Quantity),
                FirstItemTitle = first?.TitleSnapshot,
                FirstItemImageUrl = first?.ImageSnapshot,
                FirstItemListingType = first?.ListingType,
                CreatedAtUtc = order.CreatedAtUtc
            };
        }
    }
}
