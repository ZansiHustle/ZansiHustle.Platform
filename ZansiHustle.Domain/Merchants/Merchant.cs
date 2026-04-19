using System;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Domain.Merchants
{
    /// <summary>
    /// Represents a merchant/shop in the ZansiHustle ecosystem.
    /// </summary>
    public class Merchant
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// URL-safe unique slug derived from the merchant name (e.g. "urban-threads").
        /// </summary>
        public string Slug { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public MerchantType Type { get; set; }
        public MerchantStatus Status { get; set; } = MerchantStatus.Pending;
        public MerchantKycStatus KycStatus { get; set; } = MerchantKycStatus.Pending;

        public bool IsPayoutEligible { get; set; }

        public Guid? OwnerUserId { get; set; }

        // Optional shop classification — references SellerCategories taxonomy.
        public Guid? SellerCategoryId { get; set; }
        public SellerCategory? SellerCategory { get; set; }

        public Guid? SellerSubcategoryId { get; set; }
        public SellerSubcategory? SellerSubcategory { get; set; }

        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }

        // Onboarding fields promoted from the Description-stash workaround.
        // Capturing these as first-class columns so admin queries / analytics
        // / attribution reports can join on them directly.
        public string? WhatsAppNumber { get; set; }
        public string? SocialHandle { get; set; }
        public string? IdNumber { get; set; }              // sensitive — TODO encrypt at rest
        public string? ReferralCode { get; set; }
        public Guid? ReferrerUserId { get; set; }          // optional resolved owner of the referral code

        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }

        // Structured address captured from Google Places Autocomplete.
        // Province/City remain primary — these are supplementary fields the
        // picker fills in so we can render maps, filter by suburb/postal,
        // and run geo queries without re-geocoding later.
        public string? Suburb { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }       // long name, e.g. "South Africa"
        public string? CountryCode { get; set; }   // ISO-2 short name, e.g. "ZA"
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? GooglePlaceId { get; set; }
        public string? FormattedAddress { get; set; }

        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }

        public int FollowersCount { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
