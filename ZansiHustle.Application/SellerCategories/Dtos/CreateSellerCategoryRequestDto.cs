using System.Collections.Generic;

namespace ZansiHustle.Application.SellerCategories.Dtos
{
    public class CreateSellerCategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Optional initial subcategories to create together with the category.
        /// </summary>
        public List<CreateSellerSubcategoryRequestDto> Subcategories { get; set; } = new();
    }
}