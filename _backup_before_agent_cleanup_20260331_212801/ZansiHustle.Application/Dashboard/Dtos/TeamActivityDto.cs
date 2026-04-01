namespace ZansiHustle.Application.Dashboard.Dtos
{
    /// <summary>
    /// Represents a team performance summary item.
    /// </summary>
    public class TeamActivityDto
    {
        public string TeamMember { get; set; } = string.Empty;
        public int Leads { get; set; }
        public int Conversions { get; set; }
        public int Pending { get; set; }
        public long Reach { get; set; }
        public decimal Spend { get; set; }
    }
}
