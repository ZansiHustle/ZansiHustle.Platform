// CreatePublicSellerLeadRequestDto.cs
using ZansiHustle.Shared.Enums.SellerLeads;

namespace ZansiHustle.Application.SellerLeads.Dtos
{
    /// <summary>
    /// Request DTO for creating a seller lead from public website.
    /// </summary>
    public class CreatePublicSellerLeadRequestDto
    {
        /// <summary>
        /// Contact person's full name.
        /// </summary>
        public string ContactName { get; set; } = string.Empty;

        /// <summary>
        /// Business trading name.
        /// </summary>
        public string BusinessName { get; set; } = string.Empty;

        /// <summary>
        /// Type of lead (Product Seller = 0, Service Provider = 1).
        /// </summary>
        public LeadType LeadType { get; set; }

        /// <summary>
        /// Primary category of offering.
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// Subcategory if applicable.
        /// </summary>
        public string? Subcategory { get; set; }

        /// <summary>
        /// Contact phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Contact email address.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Province where business operates.
        /// </summary>
        public string? Province { get; set; }

        /// <summary>
        /// City/Town where business operates.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Social media handle or website link.
        /// </summary>
        public string? SocialHandleOrLink { get; set; }

        /// <summary>
        /// Source type (e.g., "website_become_hustler").
        /// </summary>
        public string? SourceType { get; set; }

        /// <summary>
        /// Referrer agent name (if any).
        /// </summary>
        public string? ReferrerName { get; set; }

        /// <summary>
        /// Additional notes about offering.
        /// </summary>
        public string? Notes { get; set; }
    }
}