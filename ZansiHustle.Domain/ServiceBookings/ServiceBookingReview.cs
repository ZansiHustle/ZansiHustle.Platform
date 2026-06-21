using System;
using ZansiHustle.Shared.Enums.Reviews;
using ZansiHustle.Shared.Enums.ServiceBookings;

namespace ZansiHustle.Domain.ServiceBookings
{
    /// <summary>
    /// A directional review tied to a single <see cref="ServiceBooking"/>.
    ///
    /// Distinct from the polymorphic <c>Review</c> entity because a service
    /// booking is a two-way relationship: BOTH the customer and the provider
    /// can rate each other after the service is completed. This table models the
    /// direction (<see cref="ServiceBookingReviewDirection"/>) and the reviewee
    /// explicitly, which the (TargetType, TargetId) polymorphic shape cannot.
    ///
    /// One Active review per (BookingId, Direction) — enforced by a filtered
    /// unique index and re-checked in the service layer. <see cref="Status"/>
    /// reuses <see cref="ReviewStatus"/> so soft-delete semantics match the
    /// rest of the review surface.
    /// </summary>
    public class ServiceBookingReview
    {
        public Guid Id { get; set; }

        /// <summary>FK to the booking this review is about.</summary>
        public Guid BookingId { get; set; }

        /// <summary>FK to AspNetUsers — who wrote the review.</summary>
        public Guid ReviewerUserId { get; set; }

        /// <summary>The user being reviewed (provider owner OR booking customer).</summary>
        public Guid RevieweeUserId { get; set; }

        /// <summary>Customer→Provider or Provider→Customer.</summary>
        public ServiceBookingReviewDirection Direction { get; set; }

        /// <summary>1..5, validated in the service layer + at the database (CHECK).</summary>
        public int Rating { get; set; }

        /// <summary>Optional free-text comment. Length cap enforced by the column config.</summary>
        public string? Comment { get; set; }

        public ReviewStatus Status { get; set; } = ReviewStatus.Active;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        /// <summary>Set when <see cref="Status"/> flips to <see cref="ReviewStatus.Deleted"/>.</summary>
        public DateTime? DeletedAtUtc { get; set; }
    }
}
