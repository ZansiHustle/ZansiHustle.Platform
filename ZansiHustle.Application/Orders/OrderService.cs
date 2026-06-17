using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Notifications;
using ZansiHustle.Application.Orders.Dtos;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.ServiceBookings;
using ZansiHustle.Application.Wallets;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Application.ZansiDispatch.Dtos;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Enums.ZansiDispatch;
using ZansiHustle.Shared.Enums.Wallets;
using ZansiHustle.Shared.Enums.ZansiDispatch;
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
        private readonly IWalletService _wallet;
        private readonly INotificationService _notifications;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, IListingRepository listingRepository, IServiceBookingRepository serviceBookingRepository, UserManager<User> userManager, IZansiDispatchService dispatch, IWalletService wallet, INotificationService notifications, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _listingRepository = listingRepository;
            _serviceBookingRepository = serviceBookingRepository;
            _userManager = userManager;
            _dispatch = dispatch;
            _wallet = wallet;
            _notifications = notifications;
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

                    // Address completeness guard (server-side; protects against old
                    // app versions / bad clients). A dispatchable courier option
                    // can't be fulfilled without the buyer's postal code, so reject
                    // BEFORE the order/payment is created. Collection options carry
                    // no courier and are exempt.
                    if (deliveryOption.ServiceLevel != ZansiDispatchServiceLevel.Collection
                        && string.IsNullOrWhiteSpace(deliveryOption.BuyerPostalCode))
                        return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "Delivery postal code is required before payment.");
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
                {
                    // House-call money is collected UPFRONT by the platform: the
                    // base service fee is already in `subtotal`; the surcharge +
                    // travel fee (computed server-side in BuildServiceBookingAsync)
                    // are added to the order Total here so Ozow charges the full
                    // amount. The provider is credited later from the platform
                    // balance. Buyers can NOT pay the provider off-platform.
                    var houseCallExtras = serviceBooking.HouseCallSurcharge + serviceBooking.TravelFee;
                    if (houseCallExtras > 0m)
                        order.Total += houseCallExtras;

                    await _serviceBookingRepository.AddAsync(serviceBooking);
                }

                await _orderRepository.AddAsync(order);
                var saved = await _orderRepository.SaveChangesAsync();

                if (!saved)
                    return Result<OrderDto>.Failure(ErrorCodes.Exception, "Failed to create order.");

                // NOTE: the ZansiDispatch shipment + QuoteCharged ledger entry are
                // NO LONGER created here. Creating them at order time produced
                // PendingDispatch shipments + "charged" ledger rows for orders that
                // were never paid (abandoned/failed Ozow). The shipment is now
                // created on PAYMENT SUCCESS (PaymentService.AdvanceOrderOnPaidAsync
                // → IZansiDispatchService.CreateShipmentForPaidOrderAsync), which is
                // idempotent. The selected delivery option's fee is still validated
                // + applied to Order.Total above, so the buyer pays the right amount.

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

            // Money snapshot. SERVER computes the upfront extras (never the
            // client): a house call adds the listing's house-call surcharge plus
            // the FLAT travel fee (None/PerKm contribute 0 — PerKm isn't
            // collectable until a route provider ships, so it's never charged).
            var surcharge = isHouseCall ? Math.Max(0m, listing.HouseCallSurchargeAmount ?? 0m) : 0m;
            var travelFee = isHouseCall ? ComputeFlatTravelFee(listing) : 0m;

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
                BaseServiceAmount = listing.Price,
                HouseCallSurcharge = surcharge,
                TravelFee = travelFee,
                BuyerFormattedAddress = isHouseCall ? Trim(details.BuyerFormattedAddress) : null,
                BuyerAddressLine1 = isHouseCall ? Trim(details.BuyerAddressLine1) : null,
                BuyerLatitude = isHouseCall ? details.BuyerLatitude : null,
                BuyerLongitude = isHouseCall ? details.BuyerLongitude : null,
                BuyerPlaceId = isHouseCall ? Trim(details.BuyerPlaceId) : null,
                ProviderLocationSnapshot = Trim(details.ProviderLocationSummary),
                CreatedAtUtc = nowUtc
            };

            _logger.LogInformation(
                "[ServiceBooking][Create] orderId={OrderId} listingId={ListingId} merchantId={MerchantId} startUtc={Start} endUtc={End} mode={Mode} surcharge={Surcharge} travelFee={TravelFee}",
                order.Id, listing.Id, order.MerchantId, startUtc, endUtc, mode, surcharge, travelFee);

            return Result<ServiceBooking>.Success(booking, "Booking validated.");
        }

        /// <summary>
        /// Flat travel fee charged upfront for a house-call booking, from the
        /// listing's server-side config. Only FlatFee contributes (clamped to its
        /// min/max); None and PerKilometre return 0 (PerKm is gated until a
        /// route-distance provider ships, so it's never fabricated or charged).
        /// </summary>
        private static decimal ComputeFlatTravelFee(Domain.Listings.Listing listing)
        {
            if (listing.TravelFeeType != ServiceTravelFeeType.FlatFee)
                return 0m;

            var fee = listing.TravelFeeFlatAmount ?? 0m;
            if (listing.TravelFeeMinimum is { } min && fee < min) fee = min;
            if (listing.TravelFeeMaximum is { } max && fee > max) fee = max;
            return fee < 0m ? 0m : fee;
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
                var bookingStatuses = await LoadServiceBookingStatusesAsync(orders);
                var data = orders.Select(o => MapToListItem(o, bookingStatuses)).ToList();

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
                var bookingStatuses = await LoadServiceBookingStatusesAsync(orders);
                var data = orders.Select(o => MapToListItem(o, bookingStatuses)).ToList();

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
        public async Task<Result<OrderTrackingDto>> GetTrackingAsync(Guid userId, Guid orderId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);

                if (order is null)
                    return Result<OrderTrackingDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                if (!IsBuyerOrSeller(order, userId))
                    return Result<OrderTrackingDto>.Failure(ErrorCodes.Forbidden, "You do not have permission to view this order.");

                // Customer-safe dispatch snapshot (stored state only; no seller
                // contact/address, no cost/reconciliation). Best-effort: a
                // dispatch read failure degrades to the "no dispatch yet" state
                // rather than failing the whole tracking call.
                var snapResult = await _dispatch.GetOrderDispatchSnapshotAsync(orderId);
                var snap = snapResult.IsSuccess ? snapResult.Data : null;

                // Customer-safe lifecycle updates: dispatch action-log milestones
                // (reschedule/cancellation) + payment-confirmed refund state.
                var updates = (snap?.Updates ?? new List<OrderDispatchCustomerUpdateDto>())
                    .Select(u => new OrderTrackingUpdateDto { Label = u.Label, OccurredAtUtc = u.OccurredAtUtc })
                    .ToList();
                AppendRefundUpdates(order, updates);

                var dto = new OrderTrackingDto
                {
                    OrderId = order.Id,
                    OrderCode = order.Code,
                    CurrentOrderStatus = order.Status,
                    PaymentStatus = order.PaymentStatus,
                    HasDispatch = snap?.HasShipment ?? false,
                    DispatchStatus = snap?.HasShipment == true ? snap.Status : null,
                    TrackingProvider = snap?.HasShipment == true ? snap.TrackingProvider : null,
                    TrackingNumber = snap?.HasShipment == true ? snap.TrackingNumber : null,
                    // Display ref: full tracking number, else the courier short ref.
                    TrackingReference = snap?.HasShipment == true ? snap.TrackingReference : null,
                    TrackingUrl = null, // not surfaced to customers yet
                    EstimatedDeliveryUtc = null, // provider ETA not stored yet
                    DeliveredAtUtc = snap?.DeliveredAt,
                    // Shop display name ONLY — never seller phone/address.
                    SellerDisplayName = order.Merchant?.Name,
                    // Buyer's delivery destination (NOT the seller pickup address).
                    DestinationSummary = order.DeliveryAddress,
                    Timeline = BuildTrackingTimeline(order, snap),
                    Updates = updates,
                };

                return Result<OrderTrackingDto>.Success(dto, "Tracking retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve tracking for order {OrderId} / user {UserId}.", orderId, userId);
                return Result<OrderTrackingDto>.Failure(ErrorCodes.Exception, $"Failed to retrieve tracking. {ex.Message}");
            }
        }

        // ── Customer tracking timeline ───────────────────────────────────────
        // Fixed, ordered checkpoints surfaced to the buyer. The first two are
        // order-level (paid → seller accepted); the rest collapse the dispatch
        // sub-states. A newly-paid order sits at "Seller accepted = Current"
        // (waiting for the seller); dispatch can't begin before acceptance.
        private const int StagePaid = 0;
        private const int StageAccepted = 1;
        private const int StageAwaitingDispatch = 2;
        private const int StageDelivered = 6;
        private static readonly (string Key, string Label)[] TrackingCheckpoints =
        {
            ("paid",             "Order paid"),
            ("accepted",         "Seller accepted"),
            ("awaiting_dispatch","Waiting for dispatch"),
            ("collected",        "Collected from seller"),
            ("in_transit",       "In transit"),
            ("out_for_delivery", "Out for delivery"),
            ("delivered",        "Delivered"),
        };

        /// <summary>Maps a dispatch status to its checkpoint index (2-6). Dispatch
        /// only begins after seller acceptance, so the lowest dispatch stage is
        /// "Waiting for dispatch" (2).</summary>
        private static int DispatchStageIndex(ZansiDispatchShipmentStatus status) => status switch
        {
            ZansiDispatchShipmentStatus.PendingDispatch    => 2, // waiting for dispatch
            ZansiDispatchShipmentStatus.PreparingPickup    => 2,
            ZansiDispatchShipmentStatus.BookedWithCourier  => 2, // pickup arranged, not yet collected
            ZansiDispatchShipmentStatus.PickedUp           => 3, // collected
            ZansiDispatchShipmentStatus.InTransit          => 4, // in transit
            ZansiDispatchShipmentStatus.OutForDelivery     => 5, // out for delivery
            ZansiDispatchShipmentStatus.Delivered          => 6, // delivered
            ZansiDispatchShipmentStatus.OnHold             => 2,
            _                                              => 2,
        };

        private static bool IsFailedDispatch(ZansiDispatchShipmentStatus status) =>
            status is ZansiDispatchShipmentStatus.Failed
                or ZansiDispatchShipmentStatus.Cancelled
                or ZansiDispatchShipmentStatus.Returned
                or ZansiDispatchShipmentStatus.Exception;

        /// <summary>
        /// Append CUSTOMER-SAFE refund updates driven by ACTUAL payment state — not
        /// by a cancellation request. Only a real <c>PaymentStatus.Refunded</c>
        /// surfaces a completion line; a cancelled-but-not-yet-refunded order shows
        /// "Refund processing". No raw provider/payment errors are ever exposed.
        /// </summary>
        private static void AppendRefundUpdates(Order order, List<OrderTrackingUpdateDto> updates)
        {
            var when = order.CancelledAtUtc ?? order.UpdatedAtUtc ?? order.CreatedAtUtc;

            if (order.PaymentStatus == PaymentStatus.Refunded)
            {
                // ZansiHustle refunds for cancelled orders are credited to the
                // buyer's wallet (OrderService refund path) — so a confirmed
                // Refunded state means the wallet credit completed.
                updates.Add(new OrderTrackingUpdateDto
                {
                    Label = "Refund credited to wallet",
                    OccurredAtUtc = when,
                });
            }
            else if (order.Status == OrderStatus.Cancelled && order.PaymentStatus == PaymentStatus.Paid)
            {
                // Cancelled but the money hasn't been returned yet — never claim
                // "completed" until PaymentStatus actually flips to Refunded.
                updates.Add(new OrderTrackingUpdateDto
                {
                    Label = "Refund processing",
                    OccurredAtUtc = when,
                });
            }
        }

        /// <summary>
        /// Builds the customer timeline from the order lifecycle + the stored
        /// dispatch snapshot. No live polling, no seller-private data.
        ///   • AwaitingSellerAcceptance → paid Done, "Seller accepted" Current.
        ///   • Confirmed/InProgress (accepted) → accepted Done, dispatch stages.
        ///   • Cancelled (seller rejected) → "Seller accepted" Failed + reason.
        /// </summary>
        private static List<OrderTrackingTimelineItemDto> BuildTrackingTimeline(Order order, OrderDispatchSnapshotDto? snap)
        {
            // "Paid-ish": Refunded means it WAS paid (then refunded on rejection).
            var isPaidish = order.PaymentStatus == PaymentStatus.Paid
                || order.PaymentStatus == PaymentStatus.Refunded;

            int reached;
            var failed = false;
            string? failureMsg = null;

            if (order.Status == OrderStatus.Cancelled)
            {
                // Seller rejected / order cancelled — it stalled at acceptance.
                failed = true;
                failureMsg = string.IsNullOrWhiteSpace(order.CancellationReason)
                    ? "This order was cancelled. Any payment was refunded to your wallet."
                    : $"{order.CancellationReason} Any payment was refunded to your wallet.";
                reached = isPaidish ? StageAccepted : StagePaid;
            }
            else if (order.PaymentStatus != PaymentStatus.Paid)
            {
                reached = StagePaid; // payment not secured yet
            }
            else if (order.Status == OrderStatus.AwaitingSellerAcceptance)
            {
                reached = StageAccepted; // paid Done, waiting for the seller
            }
            else if (order.Status == OrderStatus.Completed)
            {
                reached = StageDelivered;
            }
            else
            {
                // Confirmed / InProgress = seller accepted. Default to "Waiting for
                // dispatch", advancing as the dispatch snapshot progresses.
                reached = StageAwaitingDispatch;
                if (snap is { HasShipment: true } && snap.Status is { } st)
                {
                    reached = Math.Max(StageAwaitingDispatch, DispatchStageIndex(st));
                    if (IsFailedDispatch(st))
                    {
                        failed = true;
                        failureMsg = st == ZansiDispatchShipmentStatus.Returned ? "The parcel was returned. Support is on it."
                            : st == ZansiDispatchShipmentStatus.Cancelled ? "Delivery was cancelled. Support is on it."
                            : "There was a problem with delivery. Support is on it.";
                    }
                }
            }

            // occurredAt for the dispatch stages comes from the latest matching event.
            var eventTimeByStage = new Dictionary<int, DateTime>();
            if (snap?.Events is { Count: > 0 })
            {
                foreach (var ev in snap.Events)
                {
                    var idx = DispatchStageIndex(ev.InternalStatus);
                    if (!eventTimeByStage.TryGetValue(idx, out var existing) || ev.EventTime > existing)
                        eventTimeByStage[idx] = ev.EventTime;
                }
            }

            // Courier booked but not yet collected → the "awaiting dispatch" stage
            // should read "Preparing pickup", not "Waiting for dispatch".
            var dispatchBooked = snap is { HasShipment: true }
                && snap.Status is ZansiDispatchShipmentStatus.BookedWithCourier
                    or ZansiDispatchShipmentStatus.PreparingPickup;

            var timeline = new List<OrderTrackingTimelineItemDto>(TrackingCheckpoints.Length);
            for (var i = 0; i < TrackingCheckpoints.Length; i++)
            {
                var (key, label) = TrackingCheckpoints[i];

                // Once the courier is booked, relabel the awaiting-dispatch step.
                if (i == StageAwaitingDispatch && dispatchBooked)
                    label = "Preparing pickup";

                string status;
                if (i < reached)
                    status = "Done";
                else if (i == reached)
                    status = failed ? "Failed" : (reached == StageDelivered ? "Done" : "Current");
                else
                    status = "Pending";

                DateTime? occurredAt = i switch
                {
                    StagePaid => isPaidish ? order.CreatedAtUtc : null,
                    StageAccepted => order.ConfirmedAtUtc
                        ?? (order.Status == OrderStatus.Cancelled ? order.CancelledAtUtc : null),
                    StageDelivered => snap?.DeliveredAt
                        ?? (order.Status == OrderStatus.Completed ? order.CompletedAtUtc : null),
                    _ => eventTimeByStage.TryGetValue(i, out var t) ? t : (DateTime?)null,
                };

                // Friendly per-stage copy at the active step.
                string? description = null;
                if (failed && i == reached)
                    description = failureMsg;
                else if (status == "Current" && i == StageAccepted)
                    description = "Your payment is secured. We're waiting for the seller to confirm this order.";
                else if (status == "Current" && i == StageAwaitingDispatch)
                    description = dispatchBooked
                        ? "Courier has been booked and is preparing collection from the seller."
                        : "Your order has been accepted. Dispatch updates will appear here once arranged.";

                timeline.Add(new OrderTrackingTimelineItemDto
                {
                    Key = key,
                    Label = label,
                    Status = status,
                    OccurredAtUtc = occurredAt,
                    Description = description,
                });
            }

            return timeline;
        }

        /// <inheritdoc />
        public async Task<Result<OrderDto>> AcceptAsync(Guid userId, Guid orderId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order is null)
                    return Result<OrderDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                if (order.Merchant?.OwnerUserId != userId)
                    return Result<OrderDto>.Failure(ErrorCodes.Forbidden, "Only the seller can accept this order.");

                if (order.Items.Any(i => i.ListingType == ListingType.Service))
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "Service bookings are accepted from the booking screen.");

                // Concurrency/double-accept guard: must still be awaiting acceptance.
                if (order.Status != OrderStatus.AwaitingSellerAcceptance)
                    return Result<OrderDto>.Failure(ErrorCodes.Conflict, "This order can no longer be accepted.");

                if (order.PaymentStatus != PaymentStatus.Paid)
                    return Result<OrderDto>.Failure(ErrorCodes.Conflict, "This order is not paid.");

                var now = DateTime.UtcNow;
                order.Status = OrderStatus.Confirmed;
                order.ConfirmedAtUtc = now;
                order.UpdatedAtUtc = now;
                _orderRepository.Update(order);
                var saved = await _orderRepository.SaveChangesAsync();
                if (!saved)
                    return Result<OrderDto>.Failure(ErrorCodes.Exception, "Failed to accept the order.");

                // Dispatch is created ON ACCEPTANCE (not at payment) — so a courier
                // pickup is never arranged before the seller confirms fulfilment.
                // Idempotent + best-effort; only product orders with a selected
                // delivery option create a shipment.
                if (order.DeliveryQuoteOptionId is Guid deliveryOptionId)
                {
                    await _dispatch.CreateShipmentForPaidOrderAsync(
                        order.Id, order.BuyerUserId, order.MerchantId, deliveryOptionId,
                        order.DeliveryFee ?? 0m, order.DeliveryAddress);

                    // Optionally auto-book the courier now that the seller has
                    // accepted (gated behind AutoBookAfterSellerAcceptance AND the
                    // AllowShipmentBooking kill switch). Best-effort: a booking
                    // failure is recorded as NeedsAttention for ops and never fails
                    // the acceptance. NEVER runs in the payment webhook.
                    await _dispatch.AutoBookForAcceptedOrderAsync(order.Id);
                }
                else
                {
                    // No ZansiDispatch delivery option on the order → there is nothing
                    // to dispatch (collection / in-store, or the buyer's checkout never
                    // completed a delivery quote). This is NOT a failure, but we log it
                    // so "accepted but no shipment in ZansiDispatch" is diagnosable
                    // instead of silent.
                    _logger.LogInformation(
                        "Order {OrderId} accepted (Confirmed) with NO DeliveryQuoteOptionId — no ZansiDispatch shipment created (no delivery option selected at checkout).",
                        order.Id);
                }

                await _notifications.NotifyCustomerOrderAcceptedAsync(order);

                var reloaded = await _orderRepository.GetByIdAsync(order.Id);
                return Result<OrderDto>.Success(MapToDto(reloaded ?? order), "Order accepted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to accept order {OrderId} for seller {UserId}.", orderId, userId);
                return Result<OrderDto>.Failure(ErrorCodes.Exception, $"Failed to accept the order. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<OrderDto>> RejectAsync(Guid userId, Guid orderId, RejectOrderRequestDto request)
        {
            try
            {
                var reason = request?.Reason?.Trim();
                if (string.IsNullOrWhiteSpace(reason))
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "A reason is required to reject an order.");

                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order is null)
                    return Result<OrderDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                if (order.Merchant?.OwnerUserId != userId)
                    return Result<OrderDto>.Failure(ErrorCodes.Forbidden, "Only the seller can reject this order.");

                if (order.Items.Any(i => i.ListingType == ListingType.Service))
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "Service bookings are rejected from the booking screen.");

                // Concurrency/double-reject guard: rejectable only while awaiting acceptance.
                if (order.Status != OrderStatus.AwaitingSellerAcceptance)
                    return Result<OrderDto>.Failure(ErrorCodes.Conflict, "This order can no longer be rejected.");

                var now = DateTime.UtcNow;

                // Refund the customer to their wallet FIRST (idempotent + self-saving).
                // Crediting before we mark the order Refunded means a later failure
                // can't leave the order "refunded" without the money actually moving;
                // a retry re-runs this and the duplicate-credit guard makes it a no-op.
                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    var currency = string.IsNullOrWhiteSpace(order.Currency) ? "ZAR" : order.Currency;
                    if (order.Total > 0m)
                    {
                        await _wallet.CreditAsync(
                            order.BuyerUserId,
                            WalletTransactionType.OrderCancelledCredit,
                            order.Total, currency,
                            "Order", order.Id,
                            "Refund for seller-rejected order");
                    }
                    order.PaymentStatus = PaymentStatus.Refunded;
                }

                order.Status = OrderStatus.Cancelled;
                order.CancelledAtUtc = now;
                order.CancellationReason = reason;
                order.UpdatedAtUtc = now;
                _orderRepository.Update(order);
                var saved = await _orderRepository.SaveChangesAsync();
                if (!saved)
                    return Result<OrderDto>.Failure(ErrorCodes.Exception, "Failed to reject the order.");

                await _notifications.NotifyCustomerOrderRejectedAsync(order, reason);

                var reloaded = await _orderRepository.GetByIdAsync(order.Id);
                return Result<OrderDto>.Success(MapToDto(reloaded ?? order), "Order rejected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject order {OrderId} for seller {UserId}.", orderId, userId);
                return Result<OrderDto>.Failure(ErrorCodes.Exception, $"Failed to reject the order. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<OrderDto>> CancelFulfilmentAsync(Guid userId, Guid orderId, RejectOrderRequestDto? request)
        {
            try
            {
                var reason = request?.Reason?.Trim();

                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order is null)
                    return Result<OrderDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                if (order.Merchant?.OwnerUserId != userId)
                    return Result<OrderDto>.Failure(ErrorCodes.Forbidden, "Only the seller can cancel this order's fulfilment.");

                if (order.Items.Any(i => i.ListingType == ListingType.Service))
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, "Service bookings are managed from the booking screen.");

                // This is the AFTER-ACCEPTANCE path. Before acceptance the seller
                // uses Reject. Only an accepted (Confirmed/InProgress) order can have
                // its fulfilment cancelled.
                if (order.Status is not (OrderStatus.Confirmed or OrderStatus.InProgress))
                    return Result<OrderDto>.Failure(ErrorCodes.Conflict, "This order can't have its fulfilment cancelled in its current state.");

                // Ask ZansiDispatch what's safe based on the REAL shipment/provider
                // status (not a time window). This also cancels the shipment with the
                // courier when possible + writes the audit actions.
                var decision = await _dispatch.TryCancelForOrderAsync(
                    orderId, ZansiDispatchActor.Seller, userId, reason);
                if (!decision.IsSuccess || decision.Data is null)
                    return Result<OrderDto>.Failure(ErrorCodes.Exception, "Could not check the courier status. Please try again.");

                var outcome = decision.Data;
                if (!outcome.CanRefund)
                {
                    // Blocked (already collected/in transit) or provider cancel failed
                    // (needs ops). Order stays as-is; surface the customer-safe message.
                    return Result<OrderDto>.Failure(ErrorCodes.BadRequest, outcome.Message);
                }

                var now = DateTime.UtcNow;
                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    var currency = string.IsNullOrWhiteSpace(order.Currency) ? "ZAR" : order.Currency;
                    if (order.Total > 0m)
                    {
                        await _wallet.CreditAsync(
                            order.BuyerUserId,
                            WalletTransactionType.OrderCancelledCredit,
                            order.Total, currency,
                            "Order", order.Id,
                            "Refund for seller-cancelled order");
                    }
                    order.PaymentStatus = PaymentStatus.Refunded;
                }

                order.Status = OrderStatus.Cancelled;
                order.CancelledAtUtc = now;
                order.CancellationReason = string.IsNullOrWhiteSpace(reason)
                    ? "Seller cancelled after acceptance."
                    : $"Seller cancelled after acceptance: {reason}";
                order.UpdatedAtUtc = now;
                _orderRepository.Update(order);
                var saved = await _orderRepository.SaveChangesAsync();
                if (!saved)
                    return Result<OrderDto>.Failure(ErrorCodes.Exception, "Failed to cancel the order.");

                await _notifications.NotifyCustomerOrderRejectedAsync(order, reason);

                var reloaded = await _orderRepository.GetByIdAsync(order.Id);
                return Result<OrderDto>.Success(MapToDto(reloaded ?? order), outcome.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel fulfilment for order {OrderId} / seller {UserId}.", orderId, userId);
                return Result<OrderDto>.Failure(ErrorCodes.Exception, $"Failed to cancel the order. {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ShipmentDto>> ReschedulePickupAsync(Guid userId, Guid orderId, ReschedulePickupRequestDto request)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order is null)
                    return Result<ShipmentDto>.Failure(ErrorCodes.NotFound, "Order not found.");
                if (order.Merchant?.OwnerUserId != userId)
                    return Result<ShipmentDto>.Failure(ErrorCodes.Forbidden, "Only the seller can reschedule pickup for this order.");

                return await _dispatch.ReschedulePickupForOrderAsync(userId, ZansiDispatchActor.Seller, orderId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reschedule pickup for order {OrderId} / seller {UserId}.", orderId, userId);
                return Result<ShipmentDto>.Failure(ErrorCodes.Exception, "Could not request the pickup reschedule.");
            }
        }

        /// <inheritdoc />
        public async Task<Result<ShipmentDto>> RequestDeliveryChangeAsync(Guid userId, Guid orderId, RequestDeliveryChangeRequestDto request)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order is null)
                    return Result<ShipmentDto>.Failure(ErrorCodes.NotFound, "Order not found.");
                if (order.BuyerUserId != userId)
                    return Result<ShipmentDto>.Failure(ErrorCodes.Forbidden, "Only the customer can request a delivery change.");

                return await _dispatch.RequestDeliveryChangeForOrderAsync(userId, orderId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request delivery change for order {OrderId} / user {UserId}.", orderId, userId);
                return Result<ShipmentDto>.Failure(ErrorCodes.Exception, "Could not submit your delivery change request.");
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
                WalletAmountApplied = order.WalletAmountApplied,
                ExternalAmountDue = order.ExternalAmountDue,
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

        /// <summary>
        /// Batch-load the booking status for the SERVICE orders in the set — one
        /// query, no N+1. Empty when there are no service orders.
        /// </summary>
        private async Task<IReadOnlyDictionary<Guid, ServiceBookingStatus>>
            LoadServiceBookingStatusesAsync(IReadOnlyCollection<Order> orders)
        {
            var serviceOrderIds = orders
                .Where(o => o.Items.Any(i => i.ListingType == ListingType.Service))
                .Select(o => o.Id)
                .Distinct()
                .ToList();
            if (serviceOrderIds.Count == 0)
                return new Dictionary<Guid, ServiceBookingStatus>();
            return await _serviceBookingRepository.GetStatusesByOrderIdsAsync(serviceOrderIds);
        }

        private static OrderListItemDto MapToListItem(
            Order order, IReadOnlyDictionary<Guid, ServiceBookingStatus>? bookingStatuses = null)
        {
            var first = order.Items.FirstOrDefault();

            // Service booking status (legacy Confirmed → Requested for display).
            string? serviceBookingStatus = null;
            if (bookingStatuses is not null && bookingStatuses.TryGetValue(order.Id, out var bs))
            {
                serviceBookingStatus = (bs == ServiceBookingStatus.Confirmed
                    ? ServiceBookingStatus.Requested
                    : bs).ToString();
            }

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
                ServiceBookingStatus = serviceBookingStatus,
                CreatedAtUtc = order.CreatedAtUtc
            };
        }
    }
}
