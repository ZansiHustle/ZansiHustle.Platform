using System;

namespace ZansiHustle.Domain.SellerCategories
{
    /// <summary>
    /// Child subcategory mapped to a top-level seller category.
    /// </summary>
    public class SellerSubcategory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SellerCategoryId { get; set; }

        /// <summary>
        /// Display name of the subcategory.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly slug for frontend usage and analytics.
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Optional description shown in admin or future app experiences.
        /// </summary>
        public string? Description { get; set; }

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public SellerCategory? SellerCategory { get; set; }
    }
}