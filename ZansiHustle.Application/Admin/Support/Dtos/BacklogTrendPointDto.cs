namespace ZansiHustle.Application.Admin.Support.Dtos
{
    /// <summary>
    /// One datapoint in the support backlog trend chart. The chart compares
    /// opened vs resolved vs escalated volume over time — emitted even when
    /// zero so the axis stays continuous.
    /// </summary>
    public class BacklogTrendPointDto
    {
        public string Date { get; set; } = string.Empty;
        public int Opened { get; set; }
        public int Resolved { get; set; }
        public int Escalated { get; set; }
    }
}
