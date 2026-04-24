using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Admin.Users.Dtos
{
    /// <summary>
    /// One row in the admin Users view. Shape designed for internal
    /// visibility — not a public profile. UserType / MerchantType are
    /// derived by the service layer from Identity roles + Merchants
    /// table so the portal doesn't have to duplicate the classification
    /// logic.
    /// </summary>
    public class UserListItemDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        /// <summary>String form of <c>AccountStatus</c> (Active/Suspended/Inactive).</summary>
        public string AccountStatus { get; set; } = string.Empty;

        public DateTime CreatedOnUtc { get; set; }
        public DateTime? UpdatedOnUtc { get; set; }

        /// <summary>All Identity role names this user holds.</summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>Classified audience: Admin / TeamMember / Agent / Merchant / Buyer.</summary>
        public string UserType { get; set; } = UserTypeCodes.Buyer;

        /// <summary>Populated only when UserType == Merchant: Seller / ServiceProvider / StoreOwner. Null for unclassified merchants.</summary>
        public string? MerchantType { get; set; }

        // Merchant summary (populated only when a Merchants row exists for the user).
        public bool HasMerchantRecord { get; set; }
        public Guid? MerchantId { get; set; }
        public string? BusinessName { get; set; }
        public string? MerchantStatus { get; set; }
        public string? MerchantKycStatus { get; set; }
        public string? MerchantRawType { get; set; } // PhysicalStore / OnlineStore — raw enum name
    }

    /// <summary>
    /// KPI tiles for the admin users page — how the platform audience is
    /// split right now. Cheap to compute as a single grouped query.
    /// </summary>
    public class UsersKpisDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int Admins { get; set; }
        public int TeamMembers { get; set; }
        public int Agents { get; set; }
        public int Merchants { get; set; }
        public int Buyers { get; set; }
    }

    /// <summary>String constants for the top-level audience classification. Keep these in sync with the derivation logic in <c>AdminUserService</c>.</summary>
    public static class UserTypeCodes
    {
        public const string Admin      = "Admin";
        public const string TeamMember = "TeamMember";
        public const string Agent      = "Agent";
        public const string Merchant   = "Merchant";
        public const string Buyer      = "Buyer";
    }

    /// <summary>String constants for merchant subclassification.</summary>
    public static class MerchantSubTypeCodes
    {
        public const string Seller          = "Seller";
        public const string ServiceProvider = "ServiceProvider";
        public const string StoreOwner      = "StoreOwner";
    }
}
