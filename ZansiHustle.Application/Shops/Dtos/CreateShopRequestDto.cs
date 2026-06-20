using System;

namespace ZansiHustle.Application.Shops.Dtos
{
    /// <summary>
    /// Body for <c>POST /api/shops/mine</c>. Owner is taken from the
    /// JWT; merchant ownership is resolved server-side via
    /// <c>Merchant.OwnerUserId</c>.
    /// </summary>
    public class CreateShopRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public Guid? SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
        /// <summary>Curated storefront theme preset key. Null/empty → zansi_default.
        /// Unknown values are rejected with a 400.</summary>
        public string? ThemePresetKey { get; set; }
    }
}
