using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZansiHustle.Application.ServiceBookings.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ServiceBookings
{
    public interface IServiceBookingService
    {
        // ── Booking workflow ────────────────────────────────────────────────────

        /// <summary>Booking detail for the caller (must be the provider owner or
        /// the customer). Action flags are computed server-side per role.</summary>
        Task<Result<ServiceBookingDto>> GetByIdForUserAsync(Guid userId, Guid bookingId);

        /// <summary>The service booking attached to an order, for the caller
        /// (buyer or seller). Lets the buyer's order screen show booking actions.</summary>
        Task<Result<ServiceBookingDto>> GetByOrderForUserAsync(Guid userId, Guid orderId);

        /// <summary>Server-filtered bookings for the seller's shops — genuine
        /// post-payment requests/work only (never failed/stale PendingPayment).</summary>
        Task<Result<List<SellerBookingListItemDto>>> GetForSellerAsync(Guid sellerUserId);

        /// <summary>Provider accepts a requested booking (Requested → Accepted).</summary>
        Task<Result<ServiceBookingDto>> AcceptAsync(Guid userId, Guid bookingId);

        /// <summary>Provider rejects a Requested/Accepted booking with a reason.
        /// Releases the slot, credits the customer's wallet (full paid amount,
        /// idempotent), notifies the customer and records a trust event.</summary>
        Task<Result<ServiceBookingDto>> RejectAsync(Guid userId, Guid bookingId, RejectBookingRequestDto request);

        /// <summary>Either party marks the booking started. Allowed only on/after
        /// the scheduled day; flips to InProgress once BOTH sides have marked.</summary>
        Task<Result<ServiceBookingDto>> MarkInProgressAsync(Guid userId, Guid bookingId);

        /// <summary>Either party marks the booking complete. Flips to Completed
        /// once BOTH sides have marked.</summary>
        Task<Result<ServiceBookingDto>> MarkCompleteAsync(Guid userId, Guid bookingId);

        /// <summary>Customer cancels their own booking BEFORE the provider accepts
        /// (V1: Requested / legacy Confirmed only). Releases the slot, credits the
        /// customer's wallet with the full paid amount (idempotent), notifies the
        /// provider and records a trust event. NOT allowed once Accepted/InProgress/
        /// Completed — those require support (no instant-refund loophole).</summary>
        Task<Result<ServiceBookingDto>> CustomerCancelAsync(
            Guid userId, Guid bookingId, CancelBookingRequestDto request);

        // ── Quoting + availability ──────────────────────────────────────────────

        /// <summary>
        /// Compute a travel-fee quote for a house-call service from the
        /// listing's server-side fulfilment configuration. Honest by design:
        /// distance-based (PerKilometre) pricing returns "not_ready" until a
        /// road-distance provider is wired — it never fabricates a distance.
        /// </summary>
        Task<Result<TravelQuoteResultDto>> QuoteTravelAsync(TravelQuoteRequestDto request);

        /// <summary>
        /// Compute bookable availability for a service over a date range: slots
        /// generated from the seller's availability MINUS existing active
        /// bookings for the provider (so two buyers can't take the same slot).
        /// Honest by design — never invents slots a seller didn't enable, and
        /// flags when only a permissive fallback was available.
        /// </summary>
        Task<Result<AvailabilityResponseDto>> GetAvailabilityAsync(AvailabilityRequestDto request);
    }
}
