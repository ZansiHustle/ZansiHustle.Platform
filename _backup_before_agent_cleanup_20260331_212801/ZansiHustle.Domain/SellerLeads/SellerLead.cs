using System;
using ZansiHustle.Domain.Agents;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Domain.SellerLeads
{
    public class SellerLead
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
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
        public string? Notes { get; set; }
        public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAtUtc { get; set; }
        public Guid? ReviewedByUserId { get; set; }
        public Guid? AgentId { get; set; }
        public virtual Agent? Agent { get; set; }
        public virtual User? User { get; set; }
        public Guid? ConvertedSellerId { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
