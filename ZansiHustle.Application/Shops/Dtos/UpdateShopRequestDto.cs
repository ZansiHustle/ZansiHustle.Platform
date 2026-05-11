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
    }

    /// <summary>
    /// Body for <c>POST /api/shops/{shopId}/suspend</c>.
    /// </summary>
    public class SuspendShopRequestDto
    {
        public string? Reason { get; set; }
    }
}
