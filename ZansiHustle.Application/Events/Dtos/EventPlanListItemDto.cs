using System;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Application.Events.Dtos
{
    /// <summary>Compact row for the "My events" list.</summary>
    public class EventPlanListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;

        public EventType EventType { get; set; }
        public string Title { get; set; } = string.Empty;

        public DateTime? EventDate { get; set; }
        public int? GuestCount { get; set; }
        public string? LocationArea { get; set; }

        public EventPlanStatus Status { get; set; }

        public int SelectedCount { get; set; }
        public int TotalCount { get; set; }

        public decimal? BudgetTotal { get; set; }
        public decimal EstimatedTotal { get; set; }
        public string Currency { get; set; } = "ZAR";

        public DateTime CreatedAtUtc { get; set; }
    }
}
