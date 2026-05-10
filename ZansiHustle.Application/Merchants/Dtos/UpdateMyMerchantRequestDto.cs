using System;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Request model used by a seller to update a shop they own.
    /// Cannot change Status, KYC, payout eligibility, or ownership.
    /// </summary>
    /// <remarks>
    /// PATCH semantics: every field is nullable, and the service only
    /// assigns a property when the caller supplied a value (i.e.
    /// non-null). This lets focused screens — e.g. StorePhotos
    /// updating just <c>LogoUrl</c> / <c>BannerUrl</c>, or
    /// StoreDetails updating contact + address only — avoid round-
    /// tripping the whole merchant payload (which previously caused
    /// omitted fields to be nulled out).
    /// </remarks>
    public class UpdateMyMerchantRequestDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public MerchantType? Type { get; set; }
        public Guid? SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? SocialHandle { get; set; }
        public string? IdNumber { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }
        public string? Suburb { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? GooglePlaceId { get; set; }
        public string? FormattedAddress { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }
    }
}
