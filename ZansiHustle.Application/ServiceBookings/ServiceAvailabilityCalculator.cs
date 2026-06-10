using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZansiHustle.Application.ServiceBookings
{
    /// <summary>
    /// Pure, dependency-free availability maths. Mirrors the mobile rules:
    ///   • Weekdays → Mon–Fri, Weekends → Sat+Sun, Evenings → 17:00–20:00 slots,
    ///     Public Holidays → not auto-disabled, 24/7 → every day + evenings.
    ///   • No blanket Sunday block, no hard 4pm cap when evenings are enabled.
    ///   • No availability configured → permissive fallback (flagged so the
    ///     caller can warn the buyer it isn't provider-confirmed).
    /// All methods are static + side-effect free so they're trivially testable.
    /// </summary>
    public static class ServiceAvailabilityCalculator
    {
        /// <summary>Parsed seller availability flags.</summary>
        public readonly struct Availability
        {
            public bool Weekdays { get; init; }
            public bool Weekends { get; init; }
            public bool Evenings { get; init; }
            public bool PublicHolidays { get; init; }
            public bool AllDay { get; init; }
            /// <summary>Any day-scope (weekdays/weekends/24-7) was specified.</summary>
            public bool HasDayInfo { get; init; }
            /// <summary>The seller configured availability at all.</summary>
            public bool IsConfigured { get; init; }
        }

        /// <summary>One generated time slot in local SA time + its UTC window.</summary>
        public readonly struct Slot
        {
            public DateOnly Date { get; init; }
            public TimeOnly StartTime { get; init; }
            public TimeOnly EndTime { get; init; }
            public DateTime StartAtUtc { get; init; }
            public DateTime EndAtUtc { get; init; }
        }

        /// <summary>
        /// Parse the listing's free-text availability labels. Returns a struct with
        /// <c>IsConfigured == false</c> when nothing usable was provided.
        /// </summary>
        public static Availability ParseAvailability(IEnumerable<string>? labels)
        {
            var tokens = (labels ?? Enumerable.Empty<string>())
                .SelectMany(l => (l ?? string.Empty).Split(new[] { ',', '/', '|' }))
                .Select(t => t.Trim().ToLowerInvariant())
                .Where(t => t.Length > 0)
                .ToList();

            if (tokens.Count == 0)
                return default; // IsConfigured == false

            bool Has(string pattern) => tokens.Any(t => Regex.IsMatch(t, pattern));

            var allDay = Has(@"24\s*/?\s*7") || Has(@"24-7") || Has("always") || Has(@"any\s*time");
            var weekdays = allDay || Has("weekday") || Has("mon") || Has("tue") || Has("wed") || Has("thu") || Has("fri");
            var weekends = allDay || Has("weekend") || Has("sat") || Has("sun");
            var evenings = allDay || Has("evening") || Has("night") || Has(@"after\s*hours") || Has("late");
            var publicHolidays = allDay || Has("holiday") || Has("public");

            return new Availability
            {
                Weekdays = weekdays,
                Weekends = weekends,
                Evenings = evenings,
                PublicHolidays = publicHolidays,
                AllDay = allDay,
                HasDayInfo = allDay || weekdays || weekends,
                IsConfigured = true
            };
        }

        /// <summary>Whether the seller is available on the given local date.</summary>
        public static bool IsDayAvailable(DateOnly date, Availability avail)
        {
            // No signal, or only evenings/holidays specified → every day allowed
            // (the evening window still gates the times). Never blanket-blocks
            // Sundays.
            if (!avail.IsConfigured || avail.AllDay || !avail.HasDayInfo) return true;
            var dow = date.DayOfWeek;
            var isWeekend = dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday;
            return isWeekend ? avail.Weekends : avail.Weekdays;
        }

        /// <summary>
        /// Generate the candidate slots for a single local date (no booking
        /// subtraction). Day slots always; evening slots only when the seller
        /// enabled Evenings/24-7. Empty when the day isn't available.
        ///
        /// Slot starts are on the hourly interval; each slot's END is
        /// <paramref name="durationMinutes"/> after its start. A start is ONLY
        /// generated when the whole duration fits inside its window (day ends at
        /// DayLastStartHour+interval; evening at EveningLastStartHour+interval) —
        /// e.g. a 3h service in a 08:00–17:00 window offers starts up to 14:00,
        /// never 15:00 (which would end 18:00, past the window).
        /// </summary>
        public static List<Slot> GenerateSlotsForDate(DateOnly date, Availability avail, int durationMinutes)
        {
            var slots = new List<Slot>();
            if (!IsDayAvailable(date, avail)) return slots;

            var duration = durationMinutes > 0 ? durationMinutes : BookingAvailabilityDefaults.DefaultDurationMinutes;

            void AddRange(int firstHour, int lastStartHour)
            {
                // Window closes one slot-interval after the last permissible start.
                var windowEndMinutes = (lastStartHour * 60) + BookingAvailabilityDefaults.SlotIntervalMinutes;
                for (var hour = firstHour; hour <= lastStartHour; hour++)
                {
                    var startMinutes = hour * 60;
                    var endMinutes = startMinutes + duration;
                    if (endMinutes > windowEndMinutes) continue; // would overrun the window — skip

                    var start = new TimeOnly(hour, 0);
                    var end = new TimeOnly(endMinutes / 60, endMinutes % 60);
                    var startUtc = ToUtc(date, start);
                    slots.Add(new Slot
                    {
                        Date = date,
                        StartTime = start,
                        EndTime = end,
                        StartAtUtc = startUtc,
                        EndAtUtc = startUtc.AddMinutes(duration)
                    });
                }
            }

            AddRange(BookingAvailabilityDefaults.DayStartHour, BookingAvailabilityDefaults.DayLastStartHour);

            var includeEvening = avail.IsConfigured && (avail.Evenings || avail.AllDay);
            if (includeEvening)
                AddRange(BookingAvailabilityDefaults.EveningStartHour, BookingAvailabilityDefaults.EveningLastStartHour);

            return slots;
        }

        /// <summary>
        /// Resolve the effective booking duration: the seller's value clamped to
        /// [15, 720], or the 60-minute default when unset (legacy services).
        /// </summary>
        public static int ResolveDuration(int? estimatedDurationMinutes)
        {
            if (estimatedDurationMinutes is int m)
                return Math.Clamp(m, 15, 720);
            return BookingAvailabilityDefaults.DefaultDurationMinutes;
        }

        /// <summary>Convert a local SA date+time to UTC (fixed +2, no DST).</summary>
        public static DateTime ToUtc(DateOnly date, TimeOnly time)
        {
            var local = date.ToDateTime(time);
            return DateTime.SpecifyKind(local - BookingAvailabilityDefaults.SaUtcOffset, DateTimeKind.Utc);
        }

        /// <summary>
        /// Overlap test: two UTC windows overlap when each starts before the
        /// other ends. Touching edges (one ends exactly when the next starts) do
        /// NOT overlap.
        /// </summary>
        public static bool Overlaps(DateTime aStartUtc, DateTime aEndUtc, DateTime bStartUtc, DateTime bEndUtc)
            => aStartUtc < bEndUtc && aEndUtc > bStartUtc;
    }
}
