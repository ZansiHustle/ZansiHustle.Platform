using System;
using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Application.SellerLeads.Dtos
{
    /// <summary>
    /// Request model used to review a seller lead.
    /// </summary>
    public class ReviewSellerLeadRequestDto
    {
        public ApprovalStatus ApprovalStatus { get; set; }
        public string? Notes { get; set; }
        public Guid? ReviewedByUserId { get; set; }
    }
}
