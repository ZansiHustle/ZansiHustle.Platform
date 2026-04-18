namespace ZansiHustle.Application.Admin.Customers.Dtos
{
    /// <summary>
    /// Customer KPIs for the admin Customers page. Mirrors the fields the UI
    /// shows today (only <see cref="ReturningCustomerRate"/> is rendered at
    /// the moment — the others are here so future UI polish can wire them up
    /// without another backend round-trip).
    /// </summary>
    public class CustomersKpisDto
    {
        public int TotalCustomers { get; set; }
        public int ActiveThisMonth { get; set; }
        public decimal AvgOrdersPerCustomer { get; set; }
        public decimal ReturningCustomerRate { get; set; }
    }
}
