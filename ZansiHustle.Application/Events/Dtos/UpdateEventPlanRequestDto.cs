using System;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Application.Events.Dtos
{
    public class UpdateEventPlanRequestDto
    {
        public string? Title { get; set; }
        public DateTime? EventDate { get; set; }
        public int? GuestCount { get; set; }
        public string? LocationArea { get; set; }
        public decimal? BudgetTotal { get; set; }
        public string? Notes { get; set; }
        public EventPlanStatus? Status { get; set; }
    }
}
