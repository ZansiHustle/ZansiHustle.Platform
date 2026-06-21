using System;

namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// The viewer's view of the review surface for one booking. Privacy-minimal
    /// for v1: it returns ONLY the viewer's own review (<see cref="MyReview"/>),
    /// never the counterpart's, plus the flags the client needs to render the
    /// "rate / edit your rating" affordance.
    /// </summary>
    public class ServiceBookingReviewsDto
    {
        public Guid BookingId { get; set; }

        /// <summary>Booking status enum name (e.g. "Completed").</summary>
        public string BookingStatus { get; set; } = string.Empty;

        /// <summary>"customer" or "provider" — the viewer's role on this booking.</summary>
        public string ViewerRole { get; set; } = string.Empty;

        /// <summary>True when the booking is Completed AND the viewer has no
        /// existing Active review for their direction.</summary>
        public bool CanReview { get; set; }

        /// <summary>The viewer's own Active review, or null if they haven't rated.</summary>
        public ServiceBookingReviewItemDto? MyReview { get; set; }
    }
}
