using ZansiHustle.Shared.Queries;

namespace ZansiHustle.Application.Admin.Users.Dtos
{
    /// <summary>
    /// Query parameters for <c>GET /api/users</c>. Extends the standard
    /// paging/search envelope with the audience filters this module
    /// needs.
    ///
    /// All fields are optional — a naked GET returns the first page of
    /// every user.
    /// </summary>
    public class UserQueryRequestDto : PagedListQueryBase
    {
        /// <summary>
        /// Top-level audience filter. One of the <see cref="UserTypeCodes"/>
        /// values: Admin / TeamMember / Agent / Merchant / Buyer. Case-
        /// insensitive in the service layer. Null = no filter.
        /// </summary>
        public string? UserType { get; set; }

        /// <summary>
        /// Merchant subclassification filter. Only applied when <see cref="UserType"/>
        /// is "Merchant" (ignored otherwise). One of <see cref="MerchantSubTypeCodes"/>:
        /// Seller / ServiceProvider / StoreOwner.
        /// </summary>
        public string? MerchantType { get; set; }

        /// <summary>
        /// Filter by explicit Identity role name (e.g. "Agent", "SuperAdmin").
        /// Useful when <see cref="UserType"/> isn't granular enough (e.g.
        /// show only Partners among Admins). Case-insensitive.
        /// </summary>
        public string? Role { get; set; }
    }
}
