using System;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Application.Events.Dtos
{
    /// <summary>
    /// Starts a new plan. The service creates the plan row and auto-seeds
    /// checklist items from the matching <c>EventTypeTemplate</c>.
    /// </summary>
    public class CreateEventPlanRequestDto
    {
        public EventType EventType { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime? EventDate { get; set; }
        public int? GuestCount { get; set; }
        public string? LocationArea { get; set; }
        public decimal? BudgetTotal { get; set; }
        public string? Notes { get; set; }
    }
}
