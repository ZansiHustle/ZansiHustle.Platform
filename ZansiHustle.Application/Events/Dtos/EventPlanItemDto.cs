using System;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Application.Events.Dtos
{
    /// <summary>One row of the plan's checklist.</summary>
    public class EventPlanItemDto
    {
        public Guid Id { get; set; }
        public Guid EventPlanId { get; set; }

        public Guid? SellerSubcategoryId { get; set; }
        public string CategorySlug { get; set; } = string.Empty;
        public string CategoryLabel { get; set; } = string.Empty;

        public EventItemPriority Priority { get; set; }
        public int DisplayOrder { get; set; }

        public Guid? ListingId { get; set; }
        public string? ListingTitle { get; set; }
        public string? ListingImageUrl { get; set; }
        public string? MerchantName { get; set; }

        public decimal? EstimatedCost { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
