using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Application.Events.Dtos
{
    /// <summary>Full plan detail with checklist.</summary>
    public class EventPlanDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;

        public Guid UserId { get; set; }

        public EventType EventType { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
        public int? GuestCount { get; set; }
        public string? LocationArea { get; set; }
        public decimal? BudgetTotal { get; set; }
        public string Currency { get; set; } = "ZAR";
        public EventPlanStatus Status { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>Number of items that have a provider picked.</summary>
        public int SelectedCount { get; set; }

        /// <summary>Total number of items (incl. unselected slots).</summary>
        public int TotalCount { get; set; }

        /// <summary>Sum of estimated costs for items that have them set.</summary>
        public decimal EstimatedTotal { get; set; }

        public List<EventPlanItemDto> Items { get; set; } = new();
    }
}
