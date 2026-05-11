using System;

namespace ZansiHustle.Application.Shops.Dtos
{
    /// <summary>
    /// Buyer-facing shop projection. Zero billing / subscription /
    /// suspension fields — anything that's an owner/admin concern
    /// stays in <see cref="ShopProfileDto"/>. Returned by
    /// <c>GET /api/shops/public</c> and <c>GET /api/shops/{id}</c>
    /// for unauthenticated callers.
    /// </summary>
    public class ShopProfilePublicDto
    {
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? SellerCategoryName { get; set; }
        public string? SellerSubcategoryName { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
    }
}
