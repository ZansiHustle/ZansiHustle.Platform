using System;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Admin-facing request model used to create a merchant on behalf of a user.
    /// Mobile sellers use <see cref="CreateMyMerchantRequestDto"/> instead.
    /// </summary>
    public class CreateMerchantRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public MerchantType Type { get; set; }
        public Guid? OwnerUserId { get; set; }
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
