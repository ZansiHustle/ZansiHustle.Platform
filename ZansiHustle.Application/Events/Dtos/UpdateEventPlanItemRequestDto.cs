using System;

namespace ZansiHustle.Application.Events.Dtos
{
    /// <summary>
    /// Patches a single checklist slot — typically to set the chosen listing
    /// once the user picks a provider.
    /// </summary>
    public class UpdateEventPlanItemRequestDto
    {
        /// <summary>Pass <c>null</c> to clear, or a listing id to set.</summary>
        public Guid? ListingId { get; set; }

        /// <summary>Set when picking a listing; captured at pick time.</summary>
        public decimal? EstimatedCost { get; set; }

        public string? Notes { get; set; }
    }
}
