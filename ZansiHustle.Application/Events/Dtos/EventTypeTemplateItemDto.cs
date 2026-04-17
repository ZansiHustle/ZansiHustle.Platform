using System;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Application.Events.Dtos
{
    /// <summary>
    /// A single row in the checklist returned for an event type. The mobile
    /// app uses <see cref="SellerSubcategoryId"/> to filter listings; if the
    /// slug doesn't map to a real subcategory the id is null and the client
    /// can skip the provider-picker for that slot.
    /// </summary>
    public class EventTypeTemplateItemDto
    {
        public string CategorySlug { get; set; } = string.Empty;
        public string DisplayLabel { get; set; } = string.Empty;
        public EventItemPriority Priority { get; set; }
        public int DisplayOrder { get; set; }

        public Guid? SellerSubcategoryId { get; set; }
        public Guid? SellerCategoryId { get; set; }
    }
}
