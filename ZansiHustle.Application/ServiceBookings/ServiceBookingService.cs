using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Listings;
using ZansiHustle.Application.Notifications;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.Realtime;
using ZansiHustle.Application.ServiceBookings.Dtos;
using ZansiHustle.Application.Trust;
using ZansiHustle.Application.Wallets;
using ZansiHustle.Domain.ServiceBookings;
using ZansiHustle.Domain.Wallets;
using ZansiHustle.Shared.Enums.Wallets;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Notifications;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.ServiceBookings;
using ZansiHustle.Shared.Enums.Trust;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ServiceBookings
{
    /// <summary>
    /// Travel-fee quoting AND booking availability for services. Reads the
    /// listing's server-side config (so the client can't tamper) — travel fee is
    /// exact for None/FlatFee (PerKilometre stays honest "not_ready"), and
    /// availability subtracts existing active bookings from seller availability.
    /// </summary>
    public class ServiceBookingService : IServiceBookingService
    {
        private readonly IListingService _listingService;
        private readonly IListingRepository _listingRepository;
        private readonly IServiceBookingRepository _serviceBookingRepository;
        private readonly INotificationService _notifications;
        private readonly IRealtimeNotifier _realtime;
        private readonly IWalletService _wallet;
        private readonly ITrustEventService _trust;
        private readonly ILogger<ServiceBookingService> _logger;

        public ServiceBookingService(
            IListingService listingService,
            IListingRepository listingRepository,
            IServiceBookingRepository serviceBookingRepository,
            INotificationService notifications,
            IRealtimeNotifier realtime,
            IWalletService wallet,
            ITrustEventService trust,
            ILogger<ServiceBookingService> logger)
        {
            _listingService = listingService;
            _listingRepository = listingRepository;
            _serviceBookingRepository = serviceBookingRepository;
            _notifications = notifications;
            _realtime = realtime;
            _wallet = wallet;
            _trust = trust;
            _logger = logger;
        }

        // ─── Booking workflow ──────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<ServiceBookingDto>> GetByIdForUserAsync(Guid userId, Guid bookingId)
        {
            var booking = await _serviceBookingRepository.GetByIdWithDetailsAsync(bookingId);
            if (booking is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.NotFound, "Booking not found.");

            var role = ResolveRole(booking, userId);
            if (role is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Forbidden, "You can't view this booking.");

            return Result<ServiceBookingDto>.Success(BuildDto(booking, role.Value), "Booking loaded.");
        }

        /// <inheritdoc />
        public async Task<Result<ServiceBookingDto>> GetByOrderForUserAsync(Guid userId, Guid orderId)
        {
            var bookings = await _serviceBookingRepository.GetByOrderWithDetailsAsync(orderId);
            if (bookings.Count == 0)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.NotFound, "No booking for this order.");

            // v1: one service booking per order.
            var booking = bookings[0];
            var role = ResolveRole(booking, userId);
            if (role is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Forbidden, "You can't view this booking.");

            return Result<ServiceBookingDto>.Success(BuildDto(booking, role.Value), "Booking loaded.");
        }

        /// <inheritdoc />
        public async Task<Result<List<SellerBookingListItemDto>>> GetForSellerAsync(Guid sellerUserId)
        {
            if (sellerUserId == Guid.Empty)
                return Result<List<SellerBookingListItemDto>>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

            var bookings = await _serviceBookingRepository.GetForSellerAsync(sellerUserId);
            var items = bookings.Select(b =>
            {
                var localStart = b.StartAtUtc + BookingAvailabilityDefaults.SaUtcOffset;
                return new SellerBookingListItemDto
                {
                    Id = b.Id,
                    OrderId = b.OrderId,
                    ListingId = b.ListingId,
                    ServiceName = b.Listing?.Title ?? "Service",
                    Status = NormaliseStatus(b.Status),
                    PaymentStatus = b.Order?.PaymentStatus.ToString() ?? string.Empty,
                    Mode = b.Mode.ToString(),
                    Date = localStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    StartTime = localStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                    StartAtUtc = b.StartAtUtc,
                    CustomerName = b.Order?.BuyerName,
                    Amount = b.Order?.Total ?? 0m,
                    Currency = string.IsNullOrWhiteSpace(b.Order?.Currency) ? "ZAR" : b.Order!.Currency,
                    NeedsAction = IsAwaitingSeller(b.Status),
                    CreatedAtUtc = b.CreatedAtUtc
                };
            }).ToList();

            return Result<List<SellerBookingListItemDto>>.Success(items, "Bookings loaded.");
        }

        /// <inheritdoc />
        public async Task<Result<ServiceBookingDto>> AcceptAsync(Guid userId, Guid bookingId)
        {
            var booking = await _serviceBookingRepository.GetByIdWithDetailsAsync(bookingId);
            if (booking is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.NotFound, "Booking not found.");

            if (!IsProvider(booking, userId))
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Forbidden, "Only the provider can accept this booking.");

            // Requested (paid, awaiting acceptance) — legacy Confirmed treated the same.
            if (booking.Status != ServiceBookingStatus.Requested && booking.Status != ServiceBookingStatus.Confirmed)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Conflict, "This booking can no longer be accepted.");

            var nowUtc = DateTime.UtcNow;
            booking.Status = ServiceBookingStatus.Accepted;
            booking.ProviderAcceptedAtUtc = nowUtc;
            booking.ProviderAcceptedByUserId = userId;
            booking.UpdatedAtUtc = nowUtc;
            _serviceBookingRepository.Update(booking);
            await _serviceBookingRepository.SaveChangesAsync();

            var serviceName = booking.Listing?.Title ?? "your service";
            await _notifications.NotifyBookingStatusChangedAsync(
                booking, booking.CustomerUserId, NotificationType.BookingAccepted,
                "Booking accepted",
                $"Your booking for {serviceName} was accepted by the provider.");
            await BroadcastStatusAsync(booking);
            await _trust.RecordAsync(userId, TrustActorRole.Seller,
                TrustEventType.BookingAccepted, "ServiceBooking", booking.Id);

            return Result<ServiceBookingDto>.Success(BuildDto(booking, ViewerRole.Provider), "Booking accepted.");
        }

        /// <inheritdoc />
        public async Task<Result<ServiceBookingDto>> RejectAsync(
            Guid userId, Guid bookingId, RejectBookingRequestDto request)
        {
            var booking = await _serviceBookingRepository.GetByIdWithDetailsAsync(bookingId);
            if (booking is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.NotFound, "Booking not found.");

            if (!IsProvider(booking, userId))
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Forbidden, "Only the provider can reject this booking.");

            // Rejectable only before work starts: Requested/Accepted (legacy
            // Confirmed too). NOT InProgress/Completed/Cancelled/Rejected.
            if (booking.Status != ServiceBookingStatus.Requested
                && booking.Status != ServiceBookingStatus.Accepted
                && booking.Status != ServiceBookingStatus.Confirmed)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Conflict, "This booking can no longer be rejected.");

            var (reasonCode, reasonError) = NormaliseRejectionReason(request);
            if (reasonError is not null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.BadRequest, reasonError);

            var nowUtc = DateTime.UtcNow;
            booking.Status = ServiceBookingStatus.Rejected;
            booking.RejectedAtUtc = nowUtc;
            booking.RejectedByUserId = userId;
            booking.RejectionReasonCode = reasonCode;
            booking.RejectionReasonText = string.IsNullOrWhiteSpace(request.ReasonText) ? null : request.ReasonText.Trim();
            booking.UpdatedAtUtc = nowUtc;
            _serviceBookingRepository.Update(booking);
            await _serviceBookingRepository.SaveChangesAsync();

            // Credit the customer's wallet with the full paid amount (idempotent).
            // Only when the order was actually paid — nothing to refund otherwise.
            WalletTransaction? credit = null;
            var refundable = ComputeRefundable(booking);
            if (booking.Order?.PaymentStatus == PaymentStatus.Paid && refundable > 0m)
            {
                var currency = string.IsNullOrWhiteSpace(booking.Order?.Currency) ? "ZAR" : booking.Order!.Currency;
                credit = await _wallet.CreditAsync(
                    booking.CustomerUserId,
                    WalletTransactionType.BookingRejectedCredit,
                    refundable, currency,
                    "ServiceBooking", booking.Id,
                    "Refund for rejected booking");
            }

            var serviceName = booking.Listing?.Title ?? "your service";
            await _notifications.NotifyBookingStatusChangedAsync(
                booking, booking.CustomerUserId, NotificationType.BookingRejected,
                "Booking rejected",
                credit is not null
                    ? "Your booking was rejected and your wallet has been credited."
                    : $"Your booking for {serviceName} was rejected.");
            await BroadcastStatusAsync(booking);

            if (credit is not null)
                await SafeWalletBalanceChangedAsync(booking.CustomerUserId, credit.BalanceAfter, credit.Currency);

            // Trust signals (record-only; no penalties).
            await _trust.RecordAsync(userId, TrustActorRole.Seller,
                TrustEventType.BookingRejected, "ServiceBooking", booking.Id, new { reasonCode });
            await _trust.RecordAsync(booking.CustomerUserId, TrustActorRole.Customer,
                TrustEventType.BookingRejectedReceived, "ServiceBooking", booking.Id);

            return Result<ServiceBookingDto>.Success(BuildDto(booking, ViewerRole.Provider), "Booking rejected.");
        }

        /// <inheritdoc />
        public async Task<Result<ServiceBookingDto>> MarkInProgressAsync(Guid userId, Guid bookingId)
        {
            var booking = await _serviceBookingRepository.GetByIdWithDetailsAsync(bookingId);
            if (booking is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.NotFound, "Booking not found.");

            var role = ResolveRole(booking, userId);
            if (role is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Forbidden, "You can't update this booking.");

            if (booking.Status != ServiceBookingStatus.Accepted)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Conflict,
                    booking.Status == ServiceBookingStatus.InProgress
                        ? "This booking is already in progress."
                        : "This booking isn't ready to start yet.");

            if (!IsScheduledDay(booking))
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Conflict,
                    "You can start this booking on the scheduled day.");

            var nowUtc = DateTime.UtcNow;
            if (role == ViewerRole.Provider)
                booking.ProviderInProgressMarkedAtUtc ??= nowUtc;
            else
                booking.CustomerInProgressMarkedAtUtc ??= nowUtc;

            var bothMarked = booking.ProviderInProgressMarkedAtUtc is not null
                             && booking.CustomerInProgressMarkedAtUtc is not null;
            if (bothMarked)
            {
                booking.Status = ServiceBookingStatus.InProgress;
                booking.InProgressAtUtc = nowUtc;
            }
            booking.UpdatedAtUtc = nowUtc;
            _serviceBookingRepository.Update(booking);
            await _serviceBookingRepository.SaveChangesAsync();

            await NotifyOtherPartyInProgressAsync(booking, role.Value, bothMarked);
            await BroadcastStatusAsync(booking);

            return Result<ServiceBookingDto>.Success(BuildDto(booking, role.Value),
                bothMarked ? "Booking is now in progress." : "Marked as started. Waiting for the other party.");
        }

        /// <inheritdoc />
        public async Task<Result<ServiceBookingDto>> MarkCompleteAsync(Guid userId, Guid bookingId)
        {
            var booking = await _serviceBookingRepository.GetByIdWithDetailsAsync(bookingId);
            if (booking is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.NotFound, "Booking not found.");

            var role = ResolveRole(booking, userId);
            if (role is null)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Forbidden, "You can't update this booking.");

            if (booking.Status != ServiceBookingStatus.InProgress)
                return Result<ServiceBookingDto>.Failure(ErrorCodes.Conflict,
                    booking.Status == ServiceBookingStatus.Completed
                        ? "This booking is already completed."
                        : "This booking isn't in progress yet.");

            var nowUtc = DateTime.UtcNow;
            if (role == ViewerRole.Provider)
                booking.ProviderCompletedAtUtc ??= nowUtc;
            else
                booking.CustomerCompletedAtUtc ??= nowUtc;

            var bothMarked = booking.ProviderCompletedAtUtc is not null
                             && booking.CustomerCompletedAtUtc is not null;
            if (bothMarked)
            {
                booking.Status = ServiceBookingStatus.Completed;
                booking.CompletedAtUtc = nowUtc;
            }
            booking.UpdatedAtUtc = nowUtc;
            _serviceBookingRepository.Update(booking);
            await _serviceBookingRepository.SaveChangesAsync();

            await NotifyOtherPartyCompleteAsync(booking, role.Value, bothMarked);
            await BroadcastStatusAsync(booking);

            if (bothMarked)
            {
                if (booking.Merchant?.OwnerUserId is Guid sellerId && sellerId != Guid.Empty)
                    await _trust.RecordAsync(sellerId, TrustActorRole.Seller,
                        TrustEventType.BookingCompleted, "ServiceBooking", booking.Id);
                await _trust.RecordAsync(booking.CustomerUserId, TrustActorRole.Customer,
                    TrustEventType.CompletedBooking, "ServiceBooking", booking.Id);
            }

            return Result<ServiceBookingDto>.Success(BuildDto(booking, role.Value),
                bothMarked ? "Booking completed." : "Marked as complete. Waiting for the other party.");
        }

        // ─── Workflow helpers ────────────────────────────────────────────────────

        private enum ViewerRole { Provider, Customer }

        private static bool IsProvider(ServiceBooking b, Guid userId) =>
            b.Merchant?.OwnerUserId is Guid owner && owner == userId;

        private static bool IsCustomer(ServiceBooking b, Guid userId) =>
            b.CustomerUserId == userId;

        private static ViewerRole? ResolveRole(ServiceBooking b, Guid userId)
        {
            if (IsProvider(b, userId)) return ViewerRole.Provider;
            if (IsCustomer(b, userId)) return ViewerRole.Customer;
            return null;
        }

        private static bool IsScheduledDay(ServiceBooking b)
        {
            var todayLocal = DateOnly.FromDateTime(DateTime.UtcNow + BookingAvailabilityDefaults.SaUtcOffset);
            var bookingLocal = DateOnly.FromDateTime(b.StartAtUtc + BookingAvailabilityDefaults.SaUtcOffset);
            return todayLocal >= bookingLocal;
        }

        private static bool IsAwaitingSeller(ServiceBookingStatus s) =>
            s == ServiceBookingStatus.Requested || s == ServiceBookingStatus.Confirmed;

        private static string NormaliseStatus(ServiceBookingStatus s) =>
            s == ServiceBookingStatus.Confirmed
                ? ServiceBookingStatus.Requested.ToString()
                : s.ToString();

        // Allowed rejection reason codes (mirror the mobile reason sheet).
        private static readonly HashSet<string> RejectReasonCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "NotAvailable", "LocationTooFar", "CannotProvideService",
            "CustomerDetailsIncomplete", "Emergency", "Other"
        };

        /// <summary>Validate + canonicalise the rejection reason. Returns the
        /// canonical code and an error message (null when valid).</summary>
        private static (string code, string? error) NormaliseRejectionReason(RejectBookingRequestDto request)
        {
            var raw = request?.ReasonCode?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
                return (string.Empty, "A rejection reason is required.");
            if (!RejectReasonCodes.Contains(raw))
                return (string.Empty, "Choose a valid rejection reason.");

            // Canonical casing from the allowed set.
            var code = RejectReasonCodes.First(c => string.Equals(c, raw, StringComparison.OrdinalIgnoreCase));

            if (string.Equals(code, "Other", StringComparison.OrdinalIgnoreCase))
            {
                var text = request?.ReasonText?.Trim() ?? string.Empty;
                if (text.Length < 10)
                    return (code, "Please give a clear reason. Rejections are reviewed and may affect visibility.");
            }
            return (code, null);
        }

        /// <summary>Full paid amount to refund: the money snapshot
        /// (base + surcharge + travel) captured at order time, falling back to
        /// the order Total for legacy bookings created before the snapshot.</summary>
        private static decimal ComputeRefundable(ServiceBooking b)
        {
            var snapshot = b.BaseServiceAmount + b.HouseCallSurcharge + b.TravelFee;
            if (snapshot > 0m) return snapshot;
            return b.Order?.Total ?? 0m;
        }

        private async Task SafeWalletBalanceChangedAsync(Guid userId, decimal availableBalance, string currency)
        {
            try
            {
                await _realtime.WalletBalanceChangedAsync(userId, new { availableBalance, currency });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServiceBooking] wallet-balance realtime push failed for {UserId}.", userId);
            }
        }

        private async Task BroadcastStatusAsync(ServiceBooking b)
        {
            try
            {
                var payload = new
                {
                    bookingId = b.Id.ToString(),
                    status = NormaliseStatus(b.Status),
                    orderId = b.OrderId.ToString(),
                    listingId = b.ListingId.ToString(),
                    changedAtUtc = b.UpdatedAtUtc ?? DateTime.UtcNow
                };
                if (b.Merchant?.OwnerUserId is Guid owner && owner != Guid.Empty)
                    await _realtime.BookingStatusChangedAsync(owner, payload);
                await _realtime.BookingStatusChangedAsync(b.CustomerUserId, payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ServiceBooking] realtime status broadcast failed for {BookingId}.", b.Id);
            }
        }

        private async Task NotifyOtherPartyInProgressAsync(ServiceBooking b, ViewerRole actor, bool bothMarked)
        {
            var serviceName = b.Listing?.Title ?? "your service";
            if (actor == ViewerRole.Provider)
            {
                await _notifications.NotifyBookingStatusChangedAsync(
                    b, b.CustomerUserId, NotificationType.BookingInProgress,
                    bothMarked ? "Booking in progress" : "Provider is ready to start",
                    bothMarked
                        ? $"Your booking for {serviceName} is now in progress."
                        : $"The provider marked your {serviceName} booking as started. Open it to start too.");
            }
            else if (b.Merchant?.OwnerUserId is Guid owner && owner != Guid.Empty)
            {
                await _notifications.NotifyBookingStatusChangedAsync(
                    b, owner, NotificationType.BookingInProgress,
                    bothMarked ? "Booking in progress" : "Customer is ready to start",
                    bothMarked
                        ? $"The booking for {serviceName} is now in progress."
                        : $"The customer marked the {serviceName} booking as started. Open it to start too.");
            }
        }

        private async Task NotifyOtherPartyCompleteAsync(ServiceBooking b, ViewerRole actor, bool bothMarked)
        {
            var serviceName = b.Listing?.Title ?? "your service";
            if (actor == ViewerRole.Provider)
            {
                await _notifications.NotifyBookingStatusChangedAsync(
                    b, b.CustomerUserId, NotificationType.BookingCompleted,
                    bothMarked ? "Booking completed" : "Provider marked it complete",
                    bothMarked
                        ? $"Your booking for {serviceName} is complete. Tap to rate your experience."
                        : $"The provider marked your {serviceName} booking complete. Confirm to finish.");
            }
            else if (b.Merchant?.OwnerUserId is Guid owner && owner != Guid.Empty)
            {
                await _notifications.NotifyBookingStatusChangedAsync(
                    b, owner, NotificationType.BookingCompleted,
                    bothMarked ? "Booking completed" : "Customer marked it complete",
                    bothMarked
                        ? $"The booking for {serviceName} is complete."
                        : $"The customer marked the {serviceName} booking complete. Confirm to finish.");
            }
        }

        private static ServiceBookingDto BuildDto(ServiceBooking b, ViewerRole role)
        {
            var localStart = b.StartAtUtc + BookingAvailabilityDefaults.SaUtcOffset;
            var localEnd = b.EndAtUtc + BookingAvailabilityDefaults.SaUtcOffset;
            var isProvider = role == ViewerRole.Provider;
            var scheduledDay = IsScheduledDay(b);

            var providerInProgress = b.ProviderInProgressMarkedAtUtc is not null;
            var customerInProgress = b.CustomerInProgressMarkedAtUtc is not null;
            var providerComplete = b.ProviderCompletedAtUtc is not null;
            var customerComplete = b.CustomerCompletedAtUtc is not null;

            var canAccept = isProvider && IsAwaitingSeller(b.Status);
            var canMarkInProgress = b.Status == ServiceBookingStatus.Accepted
                                    && scheduledDay
                                    && (isProvider ? !providerInProgress : !customerInProgress);
            var canMarkComplete = b.Status == ServiceBookingStatus.InProgress
                                  && (isProvider ? !providerComplete : !customerComplete);
            var canRate = !isProvider && b.Status == ServiceBookingStatus.Completed;

            return new ServiceBookingDto
            {
                Id = b.Id,
                OrderId = b.OrderId,
                ListingId = b.ListingId,
                MerchantId = b.MerchantId,
                ServiceName = b.Listing?.Title ?? "Service",
                Status = NormaliseStatus(b.Status),
                PaymentStatus = b.Order?.PaymentStatus.ToString() ?? string.Empty,
                Mode = b.Mode.ToString(),
                Date = localStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                StartTime = localStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                EndTime = localEnd.ToString("HH:mm", CultureInfo.InvariantCulture),
                StartAtUtc = b.StartAtUtc,
                EndAtUtc = b.EndAtUtc,
                DurationMinutes = b.EstimatedDurationMinutes,
                Amount = b.Order?.Total ?? 0m,
                Currency = string.IsNullOrWhiteSpace(b.Order?.Currency) ? "ZAR" : b.Order!.Currency,
                CustomerName = b.Order?.BuyerName,
                CustomerPhone = b.Order?.BuyerPhone,
                Address = b.BuyerFormattedAddress ?? b.BuyerAddressLine1,
                ProviderLocationSummary = b.ProviderLocationSnapshot,
                Notes = b.Notes,
                ViewerRole = isProvider ? "provider" : "customer",
                IsScheduledDay = scheduledDay,
                ProviderMarkedInProgress = providerInProgress,
                CustomerMarkedInProgress = customerInProgress,
                ProviderMarkedComplete = providerComplete,
                CustomerMarkedComplete = customerComplete,
                CanAccept = canAccept,
                CanMarkInProgress = canMarkInProgress,
                CanMarkComplete = canMarkComplete,
                CanRate = canRate,
                RejectionReasonCode = b.RejectionReasonCode,
                RejectionReasonText = b.RejectionReasonText,
                AcceptedAtUtc = b.ProviderAcceptedAtUtc,
                InProgressAtUtc = b.InProgressAtUtc,
                CompletedAtUtc = b.CompletedAtUtc,
                RejectedAtUtc = b.RejectedAtUtc,
                CreatedAtUtc = b.CreatedAtUtc
            };
        }

        // ─── Availability ────────────────────────────────────────────────────

        /// <inheritdoc />
        public async Task<Result<AvailabilityResponseDto>> GetAvailabilityAsync(AvailabilityRequestDto request)
        {
            if (request is null || request.ListingId == Guid.Empty)
                return Result<AvailabilityResponseDto>.Failure(ErrorCodes.BadRequest, "A listing id is required.");

            var listing = await _listingRepository.GetByIdAsync(request.ListingId);
            if (listing is null)
                return Result<AvailabilityResponseDto>.Failure(ErrorCodes.NotFound, "Service not found.");
            if (listing.Type != ListingType.Service)
                return Result<AvailabilityResponseDto>.Failure(ErrorCodes.BadRequest, "Availability applies to services only.");
            if (listing.Status != ListingStatus.Active)
                return Result<AvailabilityResponseDto>.Failure(ErrorCodes.BadRequest, "This service is not currently available.");

            var nowUtc = DateTime.UtcNow;
            var todayLocal = DateOnly.FromDateTime(nowUtc + BookingAvailabilityDefaults.SaUtcOffset);
            var earliest = todayLocal.AddDays(1); // first bookable day = tomorrow
            var maxDate = todayLocal.AddDays(BookingAvailabilityDefaults.LookaheadDays);

            // Resolve + clamp the requested window into [tomorrow, today+lookahead].
            var from = request.From ?? earliest;
            if (from < earliest) from = earliest;
            var to = request.To ?? maxDate;
            if (to > maxDate) to = maxDate;
            if (to < from) to = from;

            var avail = ServiceAvailabilityCalculator.ParseAvailability(listing.Availability);

            // Effective booking duration (seller value or 60 fallback) — drives
            // slot length + window-fit, so a 3h service blocks 3h of calendar.
            var duration = ServiceAvailabilityCalculator.ResolveDuration(listing.EstimatedDurationMinutes);

            // UTC window covering the whole local range (end-exclusive next day).
            var rangeStartUtc = ServiceAvailabilityCalculator.ToUtc(from, new TimeOnly(0, 0));
            var rangeEndUtc = ServiceAvailabilityCalculator.ToUtc(to.AddDays(1), new TimeOnly(0, 0));

            var activeBookings = await _serviceBookingRepository
                .GetActiveForMerchantInRangeAsync(listing.MerchantId, rangeStartUtc, rangeEndUtc, nowUtc);

            var response = new AvailabilityResponseDto
            {
                ListingId = listing.Id,
                From = Iso(from),
                To = Iso(to),
                MaxLookaheadDays = BookingAvailabilityDefaults.LookaheadDays,
                Timezone = BookingAvailabilityDefaults.TimezoneId,
                SetupIncomplete = !avail.IsConfigured,
                UsesFallbackAvailability = !avail.IsConfigured,
                Message = avail.IsConfigured
                    ? null
                    : "This provider hasn't set their availability yet — times shown are estimates and the provider confirms after booking."
            };

            var availableDates = new HashSet<string>();

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var slots = ServiceAvailabilityCalculator.GenerateSlotsForDate(date, avail, duration);
                foreach (var slot in slots)
                {
                    var blocked = activeBookings.Any(b =>
                        ServiceAvailabilityCalculator.Overlaps(
                            slot.StartAtUtc, slot.EndAtUtc,
                            b.StartAtUtc.AddMinutes(-b.BufferMinutes),
                            b.EndAtUtc.AddMinutes(b.BufferMinutes)));

                    var isAvailable = !blocked;
                    if (isAvailable) availableDates.Add(Iso(date));

                    response.Slots.Add(new AvailabilitySlotDto
                    {
                        Date = Iso(date),
                        StartTime = Hm(slot.StartTime),
                        EndTime = Hm(slot.EndTime),
                        StartAtUtc = slot.StartAtUtc,
                        EndAtUtc = slot.EndAtUtc,
                        IsAvailable = isAvailable,
                        Reason = isAvailable ? null : "booked"
                    });
                }
            }

            response.AvailableDates = availableDates.OrderBy(d => d).ToList();

            // Booked windows that fall inside the queried range — no buyer PII.
            response.BookedSlots = activeBookings
                .Where(b => b.EndAtUtc > rangeStartUtc && b.StartAtUtc < rangeEndUtc)
                .OrderBy(b => b.StartAtUtc)
                .Select(b =>
                {
                    var localStart = b.StartAtUtc + BookingAvailabilityDefaults.SaUtcOffset;
                    var localEnd = b.EndAtUtc + BookingAvailabilityDefaults.SaUtcOffset;
                    return new BookedSlotDto
                    {
                        Date = localStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        StartTime = localStart.ToString("HH:mm", CultureInfo.InvariantCulture),
                        EndTime = localEnd.ToString("HH:mm", CultureInfo.InvariantCulture),
                        StartAtUtc = b.StartAtUtc,
                        EndAtUtc = b.EndAtUtc,
                        Status = b.Status.ToString()
                    };
                })
                .ToList();

            return Result<AvailabilityResponseDto>.Success(response, "Availability computed.");
        }

        private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string Hm(TimeOnly t) => t.ToString("HH:mm", CultureInfo.InvariantCulture);

        public async Task<Result<TravelQuoteResultDto>> QuoteTravelAsync(TravelQuoteRequestDto request)
        {
            if (request is null || request.ListingId == Guid.Empty)
                return Result<TravelQuoteResultDto>.Failure(ErrorCodes.BadRequest, "A listing id is required.");

            var listingResult = await _listingService.GetByIdAsync(request.ListingId);
            if (!listingResult.IsSuccess || listingResult.Data is null)
                return Result<TravelQuoteResultDto>.Failure(ErrorCodes.NotFound, "Service not found.");

            var listing = listingResult.Data;
            if (listing.Type != ListingType.Service)
                return Result<TravelQuoteResultDto>.Failure(ErrorCodes.BadRequest, "Travel quotes apply to services only.");

            var serviceFee = listing.Price;
            var currency = string.IsNullOrWhiteSpace(listing.Currency) ? "ZAR" : listing.Currency;
            var fulfilment = listing.Fulfilment;

            // Safe customer-facing copy shared by every not-ready reason — we
            // never expose WHY (that's the dev-only DebugMessage), just that the
            // provider confirms it. NEVER a fabricated fee.
            const string notReadyUserMessage =
                "Travel fee will be confirmed before payment.";

            TravelQuoteResultDto NotReady(string reasonCode, string debugMessage, string? userMessage = null) => new()
            {
                Status = "not_ready",
                DistanceKm = null,
                DurationMinutes = null,
                TravelFee = 0m,
                ServiceFee = serviceFee,
                Total = serviceFee,
                Currency = currency,
                Message = userMessage ?? notReadyUserMessage,
                UserMessage = userMessage ?? notReadyUserMessage,
                ReasonCode = reasonCode,
                DebugMessage = debugMessage,
            };

            TravelQuoteResultDto Ok(decimal travelFee, decimal? distanceKm, string? message = null) => new()
            {
                Status = "ok",
                DistanceKm = distanceKm,
                DurationMinutes = null,
                TravelFee = travelFee,
                ServiceFee = serviceFee,
                Total = serviceFee + travelFee,
                Currency = currency,
                Message = message,
                UserMessage = message,
            };

            // Provider hasn't configured fulfilment → honest not-ready.
            if (fulfilment is null)
                return Result<TravelQuoteResultDto>.Success(
                    NotReady("FULFILMENT_NOT_CONFIGURED",
                        "Listing has no fulfilment configured (FulfilmentMode null)."),
                    "Travel quote computed.");

            // Not a house-call service → no travel fee.
            if (!fulfilment.AllowsHouseCall)
                return Result<TravelQuoteResultDto>.Success(
                    Ok(0m, 0m, "No travel fee — this service is offered at the provider's location."),
                    "Travel quote computed.");

            switch (fulfilment.TravelFeeType)
            {
                case ServiceTravelFeeType.None:
                    return Result<TravelQuoteResultDto>.Success(
                        Ok(0m, null, "No travel fee for this service."),
                        "Travel quote computed.");

                case ServiceTravelFeeType.FlatFee:
                {
                    var fee = Clamp(
                        fulfilment.TravelFeeFlatAmount ?? 0m,
                        fulfilment.TravelFeeMinimum,
                        fulfilment.TravelFeeMaximum);
                    return Result<TravelQuoteResultDto>.Success(
                        Ok(fee, null),
                        "Travel quote computed.");
                }

                case ServiceTravelFeeType.PerKilometre:
                {
                    // Distance-based pricing needs: provider geo, buyer geo, AND a
                    // road-distance provider. Report the FIRST missing piece via a
                    // stable reason code — never straight-line-estimate a road fee.
                    var providerGeoMissing =
                        fulfilment.ProviderLocation?.Latitude is null ||
                        fulfilment.ProviderLocation?.Longitude is null;
                    if (providerGeoMissing)
                        return Result<TravelQuoteResultDto>.Success(
                            NotReady("PROVIDER_GEO_MISSING",
                                "PerKilometre requires provider lat/lng, but the listing has none."),
                            "Travel quote computed.");

                    var destinationGeoMissing =
                        request.BuyerLatitude is null || request.BuyerLongitude is null;
                    if (destinationGeoMissing)
                        return Result<TravelQuoteResultDto>.Success(
                            NotReady("DESTINATION_GEO_MISSING",
                                "PerKilometre requires buyer lat/lng, but the request didn't include it."),
                            "Travel quote computed.");

                    // Geo present on both ends, but no road-distance provider wired.
                    _logger.LogInformation(
                        "[travel-quote] per-km not_ready listing={ListingId} reason=ROUTE_PROVIDER_NOT_CONFIGURED",
                        request.ListingId);
                    return Result<TravelQuoteResultDto>.Success(
                        NotReady("ROUTE_PROVIDER_NOT_CONFIGURED",
                            "PerKilometre travel fee needs a route-distance provider (e.g. Google Routes) which isn't configured."),
                        "Travel quote computed.");
                }

                default:
                    return Result<TravelQuoteResultDto>.Success(
                        NotReady("TRAVEL_FEE_NOT_CONFIGURED",
                            $"Unconfigured/unknown travel fee type ({fulfilment.TravelFeeType})."),
                        "Travel quote computed.");
            }
        }

        private static decimal Clamp(decimal value, decimal? min, decimal? max)
        {
            if (min.HasValue && value < min.Value) value = min.Value;
            if (max.HasValue && value > max.Value) value = max.Value;
            return value < 0 ? 0 : value;
        }
    }
}
