using System;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Admin-facing request model used to update a merchant.
    /// Mobile sellers use <see cref="UpdateMyMerchantRequestDto"/> instead.
    /// </summary>
    public class UpdateMerchantRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public MerchantType Type { get; set; }
        public MerchantStatus Status { get; set; }
        public Guid? SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public decimal? Rating { get; set; }
    }
}
