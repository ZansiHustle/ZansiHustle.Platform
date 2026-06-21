using System;
using System.Threading.Tasks;
using ZansiHustle.Application.ServiceBookings.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ServiceBookings
{
    /// <summary>
    /// Two-way (customer ↔ provider) service-booking reviews. Every method
    /// resolves the caller's role from the booking and enforces:
    ///   • caller is a participant (customer OR provider owner) of the booking;
    ///   • a review can only be left once the booking is Completed;
    ///   • one Active review per direction per booking.
    /// Store-only for v1 — no Merchant/Listing aggregate is touched.
    /// </summary>
    public interface IServiceBookingReviewService
    {
        /// <summary>The viewer's review surface for a booking (their own review +
        /// can-review flag). Never returns the counterpart's review.</summary>
        Task<Result<ServiceBookingReviewsDto>> GetForBookingAsync(Guid bookingId, Guid currentUserId);

        /// <summary>Create the viewer's review for a completed booking.</summary>
        Task<Result<ServiceBookingReviewItemDto>> SubmitAsync(
            Guid bookingId, Guid currentUserId, int rating, string? comment);

        /// <summary>Edit the viewer's existing review for a booking.</summary>
        Task<Result<ServiceBookingReviewItemDto>> UpdateMineAsync(
            Guid bookingId, Guid currentUserId, int rating, string? comment);
    }
}
