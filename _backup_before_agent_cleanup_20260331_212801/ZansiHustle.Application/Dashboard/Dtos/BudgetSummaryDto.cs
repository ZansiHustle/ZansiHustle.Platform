namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents budget dashboard summary data.
    /// </summary>
    public class BudgetSummaryDto
    {
        public decimal TotalDeposits { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalObligations { get; set; }
        public decimal TotalRefunds { get; set; }
        public decimal TotalAdjustments { get; set; }
        public decimal NetAvailable { get; set; }
    }
}
