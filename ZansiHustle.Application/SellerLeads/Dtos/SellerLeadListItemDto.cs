using System;
using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Application.SellerLeads.Dtos
{
    /// <summary>
    /// Seller lead DTO for list screens. Carries enough detail for the
    /// admin / Marketplace Growth side-panel to make an approval
    /// decision without a second per-row fetch — phone, email,
    /// subcategory, social links, notes are needed at approval time.
    /// </summary>
    public class SellerLeadListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public LeadType LeadType { get; set; }
        public string? Category { get; set; }
        public string? Subcategory { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SocialHandleOrLink { get; set; }
        public string? SourceType { get; set; }
        public Guid? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public string? Notes { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ConvertedSellerId { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
