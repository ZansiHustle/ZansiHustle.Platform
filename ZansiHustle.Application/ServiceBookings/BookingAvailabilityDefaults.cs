using System;

namespace ZansiHustle.Application.ServiceBookings
{
    /// <summary>
    /// Single source of truth for booking-availability tunables. Mirrors the
    /// mobile constants (BOOKING_LOOKAHEAD_DAYS etc.) so the server and client
    /// agree on the bookable window and slot grid. Nothing here is hardcoded in
    /// more than one place.
    /// </summary>
    public static class BookingAvailabilityDefaults
    {
        /// <summary>How far ahead buyers can book (days from today).</summary>
        public const int LookaheadDays = 30;

        /// <summary>Slot granularity in minutes.</summary>
        public const int SlotIntervalMinutes = 60;

        /// <summary>Default booking duration when the listing doesn't specify one.</summary>
        public const int DefaultDurationMinutes = 60;

        /// <summary>
        /// A <see cref="ZansiHustle.Shared.Enums.ServiceBookings.ServiceBookingStatus.PendingPayment"/>
        /// booking blocks its slot only for this many minutes after creation.
        /// After that the availability endpoint ignores it (stale/abandoned
        /// checkout) so the slot is released without a background job.
        /// </summary>
        public const int PendingPaymentHoldMinutes = 20;

        /// <summary>IANA timezone label returned to clients (display/context only).</summary>
        public const string TimezoneId = "Africa/Johannesburg";

        /// <summary>
        /// South Africa is a fixed UTC+2 with NO daylight saving, so we convert
        /// local↔UTC with a constant offset rather than depending on the OS
        /// timezone database (which differs Windows vs Linux). Correct
        /// year-round for SAST.
        /// </summary>
        public static readonly TimeSpan SaUtcOffset = TimeSpan.FromHours(2);

        // Day-time grid: hourly starts 08:00 … 16:00 (last slot ends 17:00).
        public const int DayStartHour = 8;
        public const int DayLastStartHour = 16;

        // Evening grid: hourly starts 17:00 … 20:00 (only when seller enables
        // Evenings / 24-7). Removes the old hard 4pm cap.
        public const int EveningStartHour = 17;
        public const int EveningLastStartHour = 20;
    }
}
