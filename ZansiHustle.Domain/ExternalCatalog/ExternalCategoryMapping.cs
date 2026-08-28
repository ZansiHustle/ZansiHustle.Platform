using System;

namespace ZansiHustle.Domain.ExternalCatalog
{
    /// <summary>
    /// Maps an external source's own category code onto ZansiHustle's
    /// SellerCategory/SellerSubcategory. Deliberately NOT auto-matched by
    /// name/slug (both can change) — an ops-managed row is the only way a
    /// mapping exists. A product whose category code has no mapping simply
    /// syncs with a null category rather than failing (category is
    /// optional on <c>Listing</c>).
    /// </summary>
    public class ExternalCategoryMapping
    {
        public Guid Id { get; set; }
        public string SourceCode { get; set; } = string.Empty;
        public string ExternalCategoryCode { get; set; } = string.Empty;

        public Guid SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
