using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Domain.Events
{
    /// <summary>
    /// A user-owned event plan — the root of the Event Builder flow.
    /// One plan per occasion; items under it represent the checklist of
    /// service categories the user is arranging.
    /// </summary>
    public class EventPlan
    {
        public Guid Id { get; set; }

        /// <summary>Short opaque business code (e.g. <c>EVT-20260416...</c>).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Owning user (buyer / organiser).</summary>
        public Guid UserId { get; set; }

        public EventType EventType { get; set; }

        public string Title { get; set; } = string.Empty;

        /// <summary>Date the event happens (date-only; stored as UTC midnight).</summary>
        public DateTime? EventDate { get; set; }

        public int? GuestCount { get; set; }

        public string? LocationArea { get; set; }

        /// <summary>Optional total budget the user is working against. v1 stores only; no distribution logic yet.</summary>
        public decimal? BudgetTotal { get; set; }

        public string Currency { get; set; } = "ZAR";

        public EventPlanStatus Status { get; set; } = EventPlanStatus.Active;

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public List<EventPlanItem> Items { get; set; } = new();
    }
}
