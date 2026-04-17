using System;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Application.Events.Dtos
{
    /// <summary>
    /// Used to add an ad-hoc category slot to an existing plan (not part of
    /// the template). Either <c>SellerSubcategoryId</c> or <c>CategorySlug</c>
    /// must be supplied.
    /// </summary>
    public class AddEventPlanItemRequestDto
    {
        public Guid? SellerSubcategoryId { get; set; }
        public string? CategorySlug { get; set; }
        public string? CategoryLabel { get; set; }
        public EventItemPriority Priority { get; set; } = EventItemPriority.Optional;
        public int? DisplayOrder { get; set; }
        public string? Notes { get; set; }
    }
}
