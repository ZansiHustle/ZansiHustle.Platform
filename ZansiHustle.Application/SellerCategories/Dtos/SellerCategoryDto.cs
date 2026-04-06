using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class SellerCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }

        public List<SellerSubcategoryDto> Subcategories { get; set; } = new();
    }
}