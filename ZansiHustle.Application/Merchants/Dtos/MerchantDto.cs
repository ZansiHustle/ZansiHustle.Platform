using System;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Represents merchant data returned to clients.
    /// </summary>
    public class MerchantDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public MerchantType Type { get; set; }
        public MerchantStatus Status { get; set; }
        public MerchantKycStatus KycStatus { get; set; }
        public bool IsPayoutEligible { get; set; }
        public Guid? OwnerUserId { get; set; }

        public Guid? SellerCategoryId { get; set; }
        public string? SellerCategoryName { get; set; }

        public Guid? SellerSubcategoryId { get; set; }
        public string? SellerSubcategoryName { get; set; }

        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? SocialHandle { get; set; }
        public string? IdNumber { get; set; }
        public string? ReferralCode { get; set; }
        public Guid? ReferrerUserId { get; set; }
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
        /// <summary>Public seller/provider profile picture URL.</summary>
        public string? ProfileImageUrl { get; set; }
        /// <summary>
        /// Admin's reason when the application was rejected (owner/admin only —
        /// NOT on the public DTO). Lets the seller see what to fix and resubmit.
        /// </summary>
        public string? KycRejectionReason { get; set; }

        /// <summary>Bank / payout details.</summary>
        public string? BankName { get; set; }
        public string? BankAccountHolder { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountType { get; set; }
        public string? BankBranchCode { get; set; }
        public bool IsBankVerified { get; set; }
        public DateTime? BankUpdatedAtUtc { get; set; }

        public int FollowersCount { get; set; }
        /// <summary>
        /// Denormalised count of buyers who have saved this merchant.
        /// Only meaningful when <c>Type == PhysicalStore</c>.
        /// </summary>
        public int SavesCount { get; set; }
        /// <summary>True when the authenticated caller has saved this store. Always false on admin-context reads.</summary>
        public bool IsSavedByMe { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
