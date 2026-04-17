using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Domain.Events;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Infrastructure.Data.Seed
{
    /// <summary>
    /// Idempotent seeder for the <c>EventTypeTemplates</c> table. Runs on
    /// every startup; inserts any rows that are missing for the defined
    /// templates and leaves existing ones untouched so marketing edits in
    /// the DB survive redeploys.
    /// </summary>
    public static class EventTypeTemplateSeeder
    {
        private record Row(EventType EventType, string CategorySlug, string DisplayLabel, EventItemPriority Priority, int DisplayOrder);

        private static readonly Row[] Rows = new[]
        {
            // ─── Wedding ────────────────────────────────────────────────
            new Row(EventType.Wedding, "wedding-planners",    "Wedding Planner",    EventItemPriority.Essential,    0),
            new Row(EventType.Wedding, "catering",            "Catering",           EventItemPriority.Essential,    1),
            new Row(EventType.Wedding, "wedding-photography", "Photographer",       EventItemPriority.Essential,    2),
            new Row(EventType.Wedding, "decor-services",      "Decor",              EventItemPriority.Essential,    3),
            new Row(EventType.Wedding, "djs",                 "DJ",                 EventItemPriority.Recommended,  4),
            new Row(EventType.Wedding, "mcs",                 "MC",                 EventItemPriority.Recommended,  5),
            new Row(EventType.Wedding, "videography",         "Videographer",       EventItemPriority.Recommended,  6),
            new Row(EventType.Wedding, "makeup-artists",      "Makeup Artist",      EventItemPriority.Recommended,  7),
            new Row(EventType.Wedding, "shuttle-services",    "Guest Transport",    EventItemPriority.Recommended,  8),
            new Row(EventType.Wedding, "cake-makers",         "Wedding Cake",       EventItemPriority.Recommended,  9),
            new Row(EventType.Wedding, "live-bands",          "Live Band",          EventItemPriority.Optional,    10),
            new Row(EventType.Wedding, "security-services",   "Event Security",     EventItemPriority.Optional,    11),
            new Row(EventType.Wedding, "bridal-services",     "Bridal Services",    EventItemPriority.Optional,    12),

            // ─── Birthday ───────────────────────────────────────────────
            new Row(EventType.Birthday, "decor-services",     "Decor",              EventItemPriority.Essential,    0),
            new Row(EventType.Birthday, "catering",           "Catering",           EventItemPriority.Essential,    1),
            new Row(EventType.Birthday, "djs",                "DJ",                 EventItemPriority.Essential,    2),
            new Row(EventType.Birthday, "cake-makers",        "Birthday Cake",      EventItemPriority.Recommended,  3),
            new Row(EventType.Birthday, "event-photography",  "Photographer",       EventItemPriority.Recommended,  4),
            new Row(EventType.Birthday, "performers",         "Entertainment",      EventItemPriority.Recommended,  5),
            new Row(EventType.Birthday, "shuttle-services",   "Transport",          EventItemPriority.Optional,     6),
            new Row(EventType.Birthday, "mcs",                "MC",                 EventItemPriority.Optional,     7),

            // ─── Baby Shower ────────────────────────────────────────────
            new Row(EventType.BabyShower, "decor-services",    "Decor",             EventItemPriority.Essential,    0),
            new Row(EventType.BabyShower, "catering",          "Catering",          EventItemPriority.Essential,    1),
            new Row(EventType.BabyShower, "cake-makers",       "Cake",              EventItemPriority.Recommended,  2),
            new Row(EventType.BabyShower, "event-photography", "Photographer",      EventItemPriority.Recommended,  3),
            new Row(EventType.BabyShower, "performers",        "Entertainment",     EventItemPriority.Optional,     4),

            // ─── Graduation ─────────────────────────────────────────────
            new Row(EventType.Graduation, "catering",          "Catering",          EventItemPriority.Essential,    0),
            new Row(EventType.Graduation, "event-photography", "Photographer",      EventItemPriority.Essential,    1),
            new Row(EventType.Graduation, "decor-services",    "Decor",             EventItemPriority.Recommended,  2),
            new Row(EventType.Graduation, "djs",               "DJ",                EventItemPriority.Recommended,  3),
            new Row(EventType.Graduation, "shuttle-services",  "Transport",         EventItemPriority.Recommended,  4),
            new Row(EventType.Graduation, "videography",       "Videographer",      EventItemPriority.Optional,     5),
            new Row(EventType.Graduation, "mcs",               "MC",                EventItemPriority.Optional,     6),

            // ─── Funeral ────────────────────────────────────────────────
            new Row(EventType.Funeral, "funeral-services",     "Funeral Services",  EventItemPriority.Essential,    0),
            new Row(EventType.Funeral, "catering",             "Catering",          EventItemPriority.Essential,    1),
            new Row(EventType.Funeral, "tent-hire",            "Tent Hire",         EventItemPriority.Essential,    2),
            new Row(EventType.Funeral, "shuttle-services",     "Transport",         EventItemPriority.Recommended,  3),
            new Row(EventType.Funeral, "sound-hire",           "Sound Hire",        EventItemPriority.Recommended,  4),
            new Row(EventType.Funeral, "chair-table-hire",     "Chairs & Tables",   EventItemPriority.Recommended,  5),
            new Row(EventType.Funeral, "security-services",    "Security",          EventItemPriority.Optional,     6),

            // ─── Corporate ──────────────────────────────────────────────
            new Row(EventType.Corporate, "event-planners",     "Event Planner",     EventItemPriority.Essential,    0),
            new Row(EventType.Corporate, "catering",           "Catering",          EventItemPriority.Essential,    1),
            new Row(EventType.Corporate, "sound-hire",         "AV / Sound",        EventItemPriority.Essential,    2),
            new Row(EventType.Corporate, "event-photography",  "Photographer",      EventItemPriority.Recommended,  3),
            new Row(EventType.Corporate, "mcs",                "MC",                EventItemPriority.Recommended,  4),
            new Row(EventType.Corporate, "chair-table-hire",   "Chairs & Tables",   EventItemPriority.Recommended,  5),
            new Row(EventType.Corporate, "videography",        "Videographer",      EventItemPriority.Optional,     6),
            new Row(EventType.Corporate, "djs",                "DJ",                EventItemPriority.Optional,     7),
            new Row(EventType.Corporate, "shuttle-services",   "Guest Transport",   EventItemPriority.Optional,     8),
            new Row(EventType.Corporate, "security-services",  "Security",          EventItemPriority.Optional,     9),

            // ─── Other ──────────────────────────────────────────────────
            // No template — user adds categories manually. Still insert a
            // single sentinel row so GetTemplate returns an empty list cleanly
            // without needing a null-check path downstream.
        };

        public static async Task SeedAsync(AppDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var now = DateTime.UtcNow;
            var existing = await context.EventTypeTemplates
                .Select(x => new { x.EventType, x.CategorySlug })
                .ToListAsync();

            var existingSet = new HashSet<(EventType, string)>(
                existing.Select(x => (x.EventType, x.CategorySlug.ToLowerInvariant())));

            var toAdd = new List<EventTypeTemplate>();

            foreach (var row in Rows)
            {
                var key = (row.EventType, row.CategorySlug.ToLowerInvariant());
                if (existingSet.Contains(key)) continue;

                toAdd.Add(new EventTypeTemplate
                {
                    Id = Guid.NewGuid(),
                    EventType = row.EventType,
                    CategorySlug = row.CategorySlug,
                    DisplayLabel = row.DisplayLabel,
                    Priority = row.Priority,
                    DisplayOrder = row.DisplayOrder,
                    IsActive = true,
                    CreatedAtUtc = now
                });
            }

            if (toAdd.Count == 0) return;

            await context.EventTypeTemplates.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
        }
    }
}
