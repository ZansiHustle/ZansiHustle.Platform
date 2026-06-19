using System;

namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// Full service-booking detail for the seller booking screen AND the buyer
    /// order/booking screen. The SERVER computes the action flags (CanAccept /
    /// CanMarkInProgress / CanMarkComplete) from the viewer's role, the booking
    /// status, the dual-confirm flags and the day-of rule — so the client just
    /// renders buttons and never has to re-derive the lifecycle (or be trusted
    /// to enforce it).
    /// </summary>
    public class ServiceBookingDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ListingId { get; set; }
        public Guid MerchantId { get; set; }

        public string ServiceName { get; set; } = string.Empty;
        /// <summary>First listing image URL for the booked service, if any. Null when
        /// the listing has no images — the client falls back to an icon tile (no
        /// placeholder is invented server-side).</summary>
        public string? ServiceImageUrl { get; set; }

        /// <summary>Enum name: Requested/Accepted/InProgress/Completed/… The
        /// legacy Confirmed value is normalised to "Requested" for display.</summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>Order payment status enum name (Pending/Paid/Failed/Refunded).</summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>"HouseCall" | "ProviderLocation".</summary>
        public string Mode { get; set; } = string.Empty;

        // Schedule — local (SAST) display values + canonical UTC.
        public string Date { get; set; } = string.Empty;     // yyyy-MM-dd
        public string StartTime { get; set; } = string.Empty; // HH:mm
        public string EndTime { get; set; } = string.Empty;   // HH:mm
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        public int DurationMinutes { get; set; }

        // Money (charged online this round = service fee == order total).
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";

        // Customer snapshot (shown to the provider; harmless to the buyer who is
        // the customer). Buyer PII is never exposed in the public availability
        // endpoint — only here, to the two parties of the booking.
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        // Location.
        public string? Address { get; set; }
        public string? ProviderLocationSummary { get; set; }
        public string? Notes { get; set; }

        // Viewer + lifecycle.
        /// <summary>"provider" or "customer" — who is viewing.</summary>
        public string ViewerRole { get; set; } = string.Empty;
        public bool IsScheduledDay { get; set; }

        public bool ProviderMarkedInProgress { get; set; }
        public bool CustomerMarkedInProgress { get; set; }
        public bool ProviderMarkedComplete { get; set; }
        public bool CustomerMarkedComplete { get; set; }

        /// <summary>Provider may accept (Requested/legacy Confirmed, provider viewer).</summary>
        public bool CanAccept { get; set; }
        /// <summary>This viewer may mark in-progress now (Accepted + on/after the
        /// scheduled day + this side hasn't already marked it).</summary>
        public bool CanMarkInProgress { get; set; }
        /// <summary>This viewer may mark complete (InProgress + this side hasn't).</summary>
        public bool CanMarkComplete { get; set; }
        /// <summary>Customer may rate (Completed + customer viewer).</summary>
        public bool CanRate { get; set; }
        /// <summary>Customer may cancel NOW (customer viewer + still Requested/legacy
        /// Confirmed — i.e. the provider hasn't accepted yet). Instant + full credit
        /// in V1. Never true once Accepted/InProgress/Completed.</summary>
        public bool CanCustomerCancel { get; set; }

        // Rejection (set when Status == Rejected).
        public string? RejectionReasonCode { get; set; }
        public string? RejectionReasonText { get; set; }

        // Cancellation (set when Status == Cancelled).
        /// <summary>"Customer" | "Provider" | null — who cancelled the booking.</summary>
        public string? CancelledByRole { get; set; }
        public string? CancellationReasonCode { get; set; }
        public string? CancellationReasonText { get; set; }
        public DateTime? CancelledAtUtc { get; set; }

        // Timeline.
        public DateTime? AcceptedAtUtc { get; set; }
        public DateTime? InProgressAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public DateTime? RejectedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
