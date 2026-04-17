using System;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Domain.Events
{
    /// <summary>
    /// Template row driving the recommended category checklist for a given
    /// <see cref="EventType"/>. Seeded at startup; marketing-editable later
    /// without a code change.
    /// </summary>
    public class EventTypeTemplate
    {
        public Guid Id { get; set; }

        public EventType EventType { get; set; }

        /// <summary>Matches <c>SellerSubcategory.Slug</c> when possible; otherwise a virtual slug.</summary>
        public string CategorySlug { get; set; } = string.Empty;

        /// <summary>Display label shown in the checklist (e.g. "DJ", "Photographer").</summary>
        public string DisplayLabel { get; set; } = string.Empty;

        public EventItemPriority Priority { get; set; } = EventItemPriority.Recommended;

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
