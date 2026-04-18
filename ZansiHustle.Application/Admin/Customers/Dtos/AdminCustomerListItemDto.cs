namespace ZansiHustle.Application.Admin.Customers.Dtos
{
    /// <summary>
    /// One row in the admin Customers table. Derived from Orders by grouping
    /// on <c>BuyerUserId</c> — there is no stand-alone Customer aggregate in
    /// the domain today, customers are defined by their order history.
    ///
    /// <see cref="Status"/> is "active" if the buyer has placed an order in
    /// the last <see cref="ActiveWithinDays"/> days, else "inactive".
    /// <see cref="ReferredBy"/> and <see cref="City"/> are reserved fields —
    /// populated with null/empty today because the domain does not yet track
    /// referrals or per-order city.
    /// </summary>
    public class AdminCustomerListItemDto
    {
        /// <summary>Number of days back the "active" window looks — kept on
        /// the DTO so the UI can label the badge if it ever wants to.</summary>
        public const int ActiveWithinDays = 90;

        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public int Orders { get; set; }
        public decimal Spent { get; set; }
        public string Currency { get; set; } = "ZAR";

        public string? ReferredBy { get; set; }
        public string Status { get; set; } = "inactive";

        /// <summary>First-order date in ISO yyyy-MM-dd form — what the portal
        /// labels "Joined" (the customer entered our system via that order).</summary>
        public string Joined { get; set; } = string.Empty;

        /// <summary>Last-order date in ISO yyyy-MM-dd form.</summary>
        public string LastOrder { get; set; } = string.Empty;
    }
}
