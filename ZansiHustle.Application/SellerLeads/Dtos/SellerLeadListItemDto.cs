using System;
using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Application.SellerLeads.Dtos
{
    /// <summary>
    /// Lightweight seller lead DTO for list screens.
    /// </summary>
    public class SellerLeadListItemDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string BusinessName { get; set; } = string.Empty;
        public LeadType LeadType { get; set; }
        public string? Category { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? SourceType { get; set; }
        public Guid? AgentId { get; set; }
        public string? AgentName { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public DateTime SubmittedAtUtc { get; set; }
    }
}
