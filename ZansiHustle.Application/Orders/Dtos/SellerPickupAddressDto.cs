namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>
    /// Resolved seller PICKUP (collection) address surfaced on the seller's view
    /// of a product order, so the accept wizard prefills reliably instead of
    /// guessing. Resolution priority: listing → shop → merchant → seller profile
    /// (see OrderService.ResolveSellerPickupAddress). The SAME address is the one
    /// the courier booking uses (unless the seller edits it in the wizard).
    /// </summary>
    public sealed class SellerPickupAddressDto
    {
        /// <summary>Source label for display, e.g. "Shop pickup address".</summary>
        public string Label { get; set; } = "Pickup address";
        public string? StreetAddress { get; set; }
        public string? LocalArea { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        /// <summary>One-line human summary for the address card.</summary>
        public string? Summary { get; set; }
        /// <summary>True when street + city + postal code are all present (courier-ready).</summary>
        public bool IsComplete { get; set; }
    }
}
