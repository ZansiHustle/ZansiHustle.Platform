using System;
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
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public MerchantType Type { get; set; }
        public MerchantStatus Status { get; set; } = MerchantStatus.Pending;
        public MerchantKycStatus KycStatus { get; set; } = MerchantKycStatus.Pending;

        public bool IsPayoutEligible { get; set; }

        public Guid? OwnerUserId { get; set; }

        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }
        public string? AddressLine1 { get; set; }

        public string? WebsiteUrl { get; set; }
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }

        public int FollowersCount { get; set; }
        public decimal? Rating { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
