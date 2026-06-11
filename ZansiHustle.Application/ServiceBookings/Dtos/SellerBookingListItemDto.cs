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
    }
}
