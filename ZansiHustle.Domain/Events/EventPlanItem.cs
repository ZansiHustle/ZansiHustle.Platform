using System;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Shared.Enums.Events;

namespace ZansiHustle.Domain.Events
{
    /// <summary>
    /// A single slot in an <see cref="EventPlan"/> — one row per service
    /// category the organiser needs (e.g. "DJ", "Photographer"). Starts empty
    /// (no listing picked) and is filled in as the user browses providers.
    /// </summary>
    public class EventPlanItem
    {
        public Guid Id { get; set; }

        public Guid EventPlanId { get; set; }
        public EventPlan? EventPlan { get; set; }

        /// <summary>Optional SellerSubcategory match (nullable to allow ad-hoc items off-template).</summary>
        public Guid? SellerSubcategoryId { get; set; }
        public SellerSubcategory? SellerSubcategory { get; set; }

        /// <summary>
        /// Denormalised slug — kept even if the subcategory is renamed/removed so
        /// the plan stays readable. For template-seeded items this mirrors
        /// <c>EventTypeTemplate.CategorySlug</c>.
        /// </summary>
        public string CategorySlug { get; set; } = string.Empty;

        /// <summary>Display label snapshot for offline/resilience (e.g. "DJs").</summary>
        public string CategoryLabel { get; set; } = string.Empty;

        public EventItemPriority Priority { get; set; } = EventItemPriority.Recommended;

        public int DisplayOrder { get; set; }

        /// <summary>Set once the user picks a provider for this slot.</summary>
        public Guid? ListingId { get; set; }
        public Listing? Listing { get; set; }

        /// <summary>Captured at pick time — lets the plan show totals without a live listing join.</summary>
        public decimal? EstimatedCost { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
