using System;
using System.Collections.Generic;

namespace ZansiHustle.Domain.SellerCategories
{
    /// <summary>
    /// Top-level seller category used for structured marketplace/service discovery.
    /// </summary>
    public class SellerCategory
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display name of the category.
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

        /// <summary>
        /// Controls display ordering.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Allows soft disabling without deleting history.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<SellerSubcategory> Subcategories { get; set; } = new List<SellerSubcategory>();
    }
}