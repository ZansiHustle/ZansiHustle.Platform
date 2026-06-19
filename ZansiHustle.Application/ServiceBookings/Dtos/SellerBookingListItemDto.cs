using System;

namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>Lightweight row for the seller's bookings/requests list.</summary>
    public class SellerBookingListItemDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ListingId { get; set; }

        public string ServiceName { get; set; } = string.Empty;
        /// <summary>First listing image URL for the booked service, if any. Null when
        /// the listing has no images — the client falls back to an icon tile (no
        /// placeholder is invented server-side).</summary>
        public string? ServiceImageUrl { get; set; }
        /// <summary>Normalised status name (legacy Confirmed → "Requested").</summary>
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string Mode { get; set; } = string.Empty;

        public string Date { get; set; } = string.Empty;     // yyyy-MM-dd (SAST)
        public string StartTime { get; set; } = string.Empty; // HH:mm (SAST)
        public DateTime StartAtUtc { get; set; }

        public string? CustomerName { get; set; }

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";

        /// <summary>True when the booking is awaiting the seller's action
        /// (status Requested / legacy Confirmed) — drives the "needs action" dot.</summary>
        public bool NeedsAction { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        // ── Closed-booking context (only set for Cancelled/Rejected rows; the
        //    seller's "Closed" filter shows these). Lets the list card render
        //    "Cancelled by customer" / "Rejected by provider" + a reason preview
        //    without a second round-trip to the detail endpoint. ───────────────
        /// <summary>"Customer" | "Provider" | null — who cancelled (Cancelled only).</summary>
        public string? CancelledByRole { get; set; }
        /// <summary>Free-text cancellation reason preview (Cancelled only).</summary>
        public string? CancellationReasonText { get; set; }
        /// <summary>Free-text rejection reason preview (Rejected only; always provider).</summary>
        public string? RejectionReasonText { get; set; }
    }
}
