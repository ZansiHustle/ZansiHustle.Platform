using ZansiHustle.Shared.Enums.Marketplace;

namespace ZansiHustle.Application.Marketplace.Dtos
{
    /// <summary>
    /// Body for <c>PATCH /api/marketplace-listings/{id}</c>. All fields
    /// are optional; only the keys actually present in the JSON body are
    /// applied to the listing. Owner is taken from the JWT (NOT this
    /// DTO) — the service rejects with <c>FORBIDDEN</c> when the caller
    /// is not the owning user.
    ///
    /// Image management lives on a different endpoint
    /// (<c>POST /api/marketplace-listings/{id}/images</c>) so this PATCH
    /// stays focused on text/details and never has to validate or
    /// re-upload binary content.
    /// </summary>
    public class UpdateMarketplaceListingRequestDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Category { get; set; }
        public ProductCondition? Condition { get; set; }
        public string? Province { get; set; }
        public string? Location { get; set; }
        public bool? AllowOffers { get; set; }
    }
}
