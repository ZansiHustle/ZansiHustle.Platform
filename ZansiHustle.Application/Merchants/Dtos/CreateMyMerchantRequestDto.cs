using System;
using System.Collections.Generic;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Request model used by a seller to create a shop they own.
    /// Server sets <c>OwnerUserId</c> from the JWT; clients cannot spoof it.
    /// Status, KYC, and payout eligibility are server-managed.
    /// </summary>
    public class CreateMyMerchantRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public MerchantType Type { get; set; } = MerchantType.OnlineStore;
        public Guid? SellerCategoryId { get; set; }
        public Guid? SellerSubcategoryId { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? SocialHandle { get; set; }
        public string? IdNumber { get; set; }
        public string? ReferralCode { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }

        // Structured address from Google Places Autocomplete. All optional;
        // Province/City above remain the primary fields. The picker fills
        // everything in one go so the seller doesn't enter duplicate data.
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

        /// <summary>
        /// Ids of MediaAsset rows the client uploaded during onboarding
        /// (ID document, portrait/selfie, product sample, etc.). The
        /// service re-parents them onto the new Merchant.
        /// </summary>
        public List<Guid> MediaAssetIds { get; set; } = new();
    }
}
