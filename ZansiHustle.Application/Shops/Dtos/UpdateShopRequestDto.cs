using System;

namespace ZansiHustle.Application.Shops.Dtos
{
    /// <summary>
    /// Body for <c>PATCH /api/shops/mine/{shopId}</c>. PATCH semantics:
    /// only supplied (non-null) fields are applied; omitted fields stay
    /// as-is on the row. Service layer enforces ownership.
    /// </summary>
    public class UpdateShopRequestDto
    {
        public string? Name { get; set; }
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
        /// <summary>Curated storefront theme preset key. Omit to keep the current
        /// value; an unknown value is rejected with a 400.</summary>
        public string? ThemePresetKey { get; set; }
        /// <summary>Storefront background mode (light/themed/dark). Omit to keep the
        /// current value; an unknown value is rejected with a 400.</summary>
        public string? ThemeBackgroundMode { get; set; }
    }

    /// <summary>
    /// Body for <c>POST /api/shops/{shopId}/suspend</c>.
    /// </summary>
    public class SuspendShopRequestDto
    {
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Body for <c>PUT /api/shops/mine/{shopId}/visibility</c>. Seller pause /
    /// resume of the shop's buyer-facing visibility.
    /// </summary>
    public class ShopVisibilityRequestDto
    {
        public bool IsPaused { get; set; }
        public string? Reason { get; set; }
    }
}
