using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Orders.Dtos;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.ServiceBookings;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Application.ZansiDispatch.Dtos;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IServiceBookingRepository _serviceBookingRepository;
        private readonly UserManager<User> _userManager;
        private readonly IZansiDispatchService _dispatch;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IListingRepository listingRepository, IServiceBookingRepository serviceBookingRepository, UserManager<User> userManager, IZansiDispatchService dispatch, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _listingRepository = listingRepository;
            _serviceBookingRepository = serviceBookingRepository;
            _userManager = userManager;
            _dispatch = dispatch;
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

                    // ── Stock validation ────────────────────────────────────
                    // `Listing.Stock` is nullable — null means "not tracked"
                    // (typical for services + open-stock products). When it
                    // IS tracked, requested quantity must fit. The check uses
                    // the grouped quantity, so two cart lines of the same
                    // listing collapse first → no false "in stock" because
                    // each line individually fits while the sum doesn't.
                    //
                    // No reservation / decrement here — that requires a
                    // transactional inventory model with refund-on-cancel
                    // semantics. Pre-checking is enough to stop the obvious
                    // overselling case at the moment of order placement.
                    // Variant-level stock (ListingVariant.Stock) is NOT
                    // consulted yet because OrderItem can't yet carry a
                    // VariantId — see "Variant order support" follow-up.
                    if (listing.Stock is int available)
                    {
                        if (available <= 0)
                            return Result<OrderDto>.Failure(
                                ErrorCodes.BadRequest,
                                $"'{listing.Title}' is out of stock.");

                        if (line.Quantity > available)
                            return Result<OrderDto>.Failure(
                                ErrorCodes.BadRequest,
                                $"Only {available} left in stock for '{listing.Title}'.");
                    }

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

                // ── ZansiDispatch delivery (optional, additive) ──────────────
                // When checkout passed a selected delivery quote option, resolve
                // + validate it BEFORE building the order so a bad/expired
                // option fails fast and is never charged. When absent, delivery
                // stays null and Total == Subtotal exactly as before.
                SelectableQuoteOptionDto? deliveryOption = null;
                if (request.DeliveryQuoteOptionId is Guid deliveryOptionId)
                {
                    var optionResult = await _dispatch.GetSelectableOptionAsync(buyerUserId, deliveryOptionId);
                    if (!optionResult.IsSuccess || optionResult.Data is null)
                        return Result<OrderDto>.Failure(optionResult.Code, optionResult.Message);
                    deliveryOption = optionResult.Data;
                }

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

                // Delivery fee comes from the validated ZansiDispatch option
                // (server-side; never trusted from the client). No option →
                // null fee, Total == Subtotal (unchanged legacy behaviour).
                var deliveryFee = deliveryOption?.Amount ?? 0m;
                order.Subtotal = subtotal;
                order.DeliveryFee = deliveryOption is null ? (decimal?)null : deliveryFee;
                order.DeliveryQuoteOptionId = deliveryOption?.QuoteOptionId;
                order.Total = subtotal + deliveryFee;

                // ── Service booking (optional, additive) ─────────────────────
                // When the buyer is booking a service slot, validate it is STILL
                // available server-side (anti-double-book race guard) and build
                // the ServiceBooking row. Added to the SAME DbContext as the
                // order below so they commit atomically — a taken slot fails the
                // whole create and no orphan order is left behind.
                ServiceBooking? serviceBooking = null;
                if (request.ServiceBooking is not null)
                {
                    var bookingResult = await BuildServiceBookingAsync(order, listings, buyerUserId, request.ServiceBooking);
                    if (!bookingResult.IsSuccess)
                        return Result<OrderDto>.Failure(bookingResult.Code, bookingResult.Message);
                    serviceBooking = bookingResult.Data;
                }

                if (serviceBooking is not null)
                    await _serviceBookingRepository.AddAsync(serviceBooking);

                await _orderRepository.AddAsync(order);
                var saved = await _orderRepository.SaveChangesAsync();

                if (!saved)
                    return Result<OrderDto>.Failure(ErrorCodes.Exception, "Failed to create order.");

                // Spin up the shipment from the selected option (post-save,
                // best-effort — never fails the order if logistics hiccups).
                if (deliveryOption is not null)
                {
                    await _dispatch.CreateShipmentForOrderAsync(new CreateShipmentForOrderInput
                    {
                        OrderId = order.Id,
                        UserId = buyerUserId,
                        MerchantId = merchantId,
                        ShopId = deliveryOption.ShopId,
                        QuoteId = deliveryOption.QuoteId,
                        QuoteOptionId = deliveryOption.QuoteOptionId,
                        ProviderType = deliveryOption.ProviderType,
                        ServiceLevel = deliveryOption.ServiceLevel,
                        QuotedDeliveryFee = deliveryFee,
                        PickupAddressSummary = deliveryOption.PickupAddressSummary,
                        DropoffAddressSummary = deliveryOption.DropoffAddressSummary ?? order.DeliveryAddress,
                    });
                }

                var reloaded = await _orderRepository.GetByIdAsync(order.Id);

                // Success log. Safe fields only — id/code/status/totals/item count/
                // merchant id. No buyer email/phone, no listing snapshots, no
                // delivery address. Anyone debugging "where did my order go?"
                // from Swagger / UAT just searches for the orderCode.
                _logger.LogInformation(
                    "[Orders][Create] OK orderId={OrderId} orderCode={OrderCode} buyerUserId={BuyerId} merchantId={MerchantId} status={Status} paymentStatus={PaymentStatus} subtotal={Subtotal} deliveryFee={DeliveryFee} total={Total} {Currency} itemCount={ItemCount}",
                    order.Id, order.Code, buyerUserId, merchantId, order.Status, order.PaymentStatus,
                    order.Subtotal, order.DeliveryFee, order.Total, order.Currency, order.Items.Count);

                return Result<OrderDto>.Success(MapToDto(reloaded ?? order), "Order placed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order create failed for buyer {BuyerId}.", buyerUserId);
                // Keep buyer-facing message generic; detail stays in the log.
                return Result<OrderDto>.Failure(ErrorCodes.Exception, "Failed to place order.");
            }
        }

        /// <summary>
        /// Validate a requested service slot server-side and build the booking
        /// row. Returns a failure Result (no order is saved) when the date is out
        /// of range, the provider isn't available that day, or the slot was taken
        /// since the buyer last loaded availability (race guard).
        /// </summary>
        private async Task<Result<ServiceBooking>> BuildServiceBookingAsync(
            Order order, List<Domain.Listings.Listing> listings, Guid buyerUserId, ServiceBookingDetailsDto details)
        {
            var services = listings.Where(l => l.Type == ListingType.Service).ToList();
            if (services.Count != 1)
                return Result<ServiceBooking>.Failure(ErrorCodes.BadRequest, "Booking details require a single service in the order.");

            var listing = services[0];

            if (!DateOnly.TryParseExact(details.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                return Result<ServiceBooking>.Failure(ErrorCodes.BadRequest, "Invalid booking date.");
            if (!TryParseTime(details.Time, out var time))
                return Result<ServiceBooking>.Failure(ErrorCodes.BadRequest, "Invalid booking time.");

            var nowUtc = DateTime.UtcNow;
            var todayLocal = DateOnly.FromDateTime(nowUtc + BookingAvailabilityDefaults.SaUtcOffset);
            var earliest = todayLocal.AddDays(1);
            var latest = todayLocal.AddDays(BookingAvailabilityDefaults.LookaheadDays);
            if (date < earliest || date > latest)
                return Result<ServiceBooking>.Failure(
                    ErrorCodes.BadRequest,
                    $"Please choose a date within the next {BookingAvailabilityDefaults.LookaheadDays} days.");

            var avail = ServiceAvailabilityCalculator.ParseAvailability(listing.Availability);
            if (avail.IsConfigured && !ServiceAvailabilityCalculator.IsDayAvailable(date, avail))
                return Result<ServiceBooking>.Failure(
                    ErrorCodes.Conflict,
                    "The provider isn't available on that day. Please choose another slot.");

            // SERVER owns the duration — the listing's value (or 60 fallback),
            // NEVER the client's. Otherwise a buyer could shrink durationMinutes
            // to dodge calendar blocking. `details.DurationMinutes` is display-only.
            var duration = ServiceAvailabilityCalculator.ResolveDuration(listing.EstimatedDurationMinutes);

            // The requested start must be a real generated slot for this date +
            // duration (enforces window-fit + day rules server-side).
            var slots = ServiceAvailabilityCalculator.GenerateSlotsForDate(date, avail, duration);
            if (!slots.Any(s => s.StartTime == time))
                return Result<ServiceBooking>.Failure(
                    ErrorCodes.Conflict,
                    "That time isn't available for this service. Please choose another slot.");

            var buffer = Math.Max(0, listing.BufferMinutes ?? 0);
            var startUtc = ServiceAvailabilityCalculator.ToUtc(date, time);
            var endUtc = startUtc.AddMinutes(duration);

            // Server-side anti-double-book guard — the authoritative check.
            var overlaps = await _serviceBookingRepository.HasActiveOverlapAsync(
                order.MerchantId, startUtc, endUtc, nowUtc);
            if (overlaps)
                return Result<ServiceBooking>.Failure(
                    ErrorCodes.Conflict,
                    "This time is no longer available. Please choose another slot.");

            var isHouseCall = (details.Mode ?? string.Empty).Contains("house", StringComparison.OrdinalIgnoreCase);
            var mode = isHouseCall ? ServiceBookingMode.HouseCall : ServiceBookingMode.ProviderLocation;
            var orderItem = order.Items.FirstOrDefault(i => i.ListingId == listing.Id);

            var booking = new ServiceBooking
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                OrderItemId = orderItem?.Id,
                ListingId = listing.Id,
                MerchantId = order.MerchantId,
                CustomerUserId = buyerUserId,
                StartAtUtc = startUtc,
                EndAtUtc = endUtc,
                EstimatedDurationMinutes = duration,
                BufferMinutes = buffer,
                Mode = mode,
                Status = ServiceBookingStatus.PendingPayment,
                BuyerFormattedAddress = isHouseCall ? Trim(details.BuyerFormattedAddress) : null,
                BuyerAddressLine1 = isHouseCall ? Trim(details.BuyerAddressLine1) : null,
                BuyerLatitude = isHouseCall ? details.BuyerLatitude : null,
                BuyerLongitude = isHouseCall ? details.BuyerLongitude : null,
                BuyerPlaceId = isHouseCall ? Trim(details.BuyerPlaceId) : null,
                ProviderLocationSnapshot = Trim(details.ProviderLocationSummary),
                CreatedAtUtc = nowUtc
            };

            _logger.LogInformation(
                "[ServiceBooking][Create] orderId={OrderId} listingId={ListingId} merchantId={MerchantId} startUtc={Start} endUtc={End} mode={Mode}",
                order.Id, listing.Id, order.MerchantId, startUtc, endUtc, mode);

            return Result<ServiceBooking>.Success(booking, "Booking validated.");
        }

        private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static bool TryParseTime(string? raw, out TimeOnly time)
        {
            time = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            return TimeOnly.TryParseExact(
                raw.Trim(), new[] { "HH:mm", "H:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
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

                if (isSeller && !isBuyer && order.Merchant?.Status != ZansiHustle.Shared.Enums.Merchants.MerchantStatus.Active)
                    return Result<OrderDto>.Failure(ErrorCodes.Forbidden, "Your shop must be approved before you can manage orders.");

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
                DeliveryFee = order.DeliveryFee,
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
