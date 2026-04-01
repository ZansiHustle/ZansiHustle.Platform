using System;
using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Application.SellerLeads.Dtos
{
    /// <summary>
    /// Request model used to update a seller lead.
    /// </summary>
    public class UpdateSellerLeadRequestDto
    {
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
        public Guid? AgentId { get; set; }
        public string? Notes { get; set; }
    }
}
