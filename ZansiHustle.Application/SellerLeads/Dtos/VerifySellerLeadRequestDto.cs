using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Application.SellerLeads.Dtos
{
    /// <summary>
    /// Request model used to update lead verification status.
    /// </summary>
    public class VerifySellerLeadRequestDto
    {
        public VerificationStatus VerificationStatus { get; set; }
        public string? Notes { get; set; }
    }
}
