namespace ZansiHustle.Application.Admin.Orders.Dtos
{
    /// <summary>
    /// One row in the admin Orders table. Shape mirrors what the portal grid
    /// already renders so no client-side mapping is needed. <see cref="Status"/>
    /// is the lowercase/underscored enum name (e.g. <c>"pending"</c>,
    /// <c>"in_progress"</c>) to match the convention already used by the
    /// merchant portal store.
    /// </summary>
    public class AdminOrderListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string Shop { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
    }
}
