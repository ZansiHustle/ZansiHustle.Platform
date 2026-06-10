using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// Request to read a service's bookable availability over a date range.
    /// </summary>
    public class AvailabilityRequestDto
    {
        public Guid ListingId { get; set; }
        /// <summary>Inclusive local start date (yyyy-MM-dd). Defaults to tomorrow.</summary>
        public DateOnly? From { get; set; }
        /// <summary>Inclusive local end date (yyyy-MM-dd). Clamped to lookahead.</summary>
        public DateOnly? To { get; set; }
    }

    /// <summary>A single generated slot (mobile-friendly — local times + UTC).</summary>
    public class AvailabilitySlotDto
    {
        public string Date { get; set; } = string.Empty;       // yyyy-MM-dd
        public string StartTime { get; set; } = string.Empty;  // HH:mm
        public string EndTime { get; set; } = string.Empty;    // HH:mm
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        public bool IsAvailable { get; set; }
        /// <summary>Why a slot is unavailable ("booked"), else null.</summary>
        public string? Reason { get; set; }
    }

    /// <summary>A blocked window (existing booking). No buyer PII is exposed.</summary>
    public class BookedSlotDto
    {
        public string Date { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public DateTime StartAtUtc { get; set; }
        public DateTime EndAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>Availability response for the buyer booking calendar.</summary>
    public class AvailabilityResponseDto
    {
        public Guid ListingId { get; set; }
        public string From { get; set; } = string.Empty;       // yyyy-MM-dd
        public string To { get; set; } = string.Empty;         // yyyy-MM-dd
        public int MaxLookaheadDays { get; set; }
        public string Timezone { get; set; } = string.Empty;

        /// <summary>Dates with at least one available slot.</summary>
        public List<string> AvailableDates { get; set; } = new();
        public List<AvailabilitySlotDto> Slots { get; set; } = new();
        public List<BookedSlotDto> BookedSlots { get; set; } = new();

        /// <summary>True when the seller hasn't configured availability at all.</summary>
        public bool SetupIncomplete { get; set; }
        /// <summary>True when a permissive fallback (not provider-confirmed) was used.</summary>
        public bool UsesFallbackAvailability { get; set; }
        public string? Message { get; set; }
    }
}
