using System;

namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class SellerSubcategoryDto
    {
        public Guid Id { get; set; }
        public Guid SellerCategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}