namespace ZansiHustle.Application.Admin.Support.Dtos
{
    /// <summary>
    /// Admin Support KPI snapshot. Covers the tracking metrics the user
    /// explicitly requested (totals + average resolution time) plus the
    /// supporting tiles the SupportDashboard page already renders (SLA,
    /// CSAT, response time, disputes awaiting review).
    ///
    /// No Ticket/Dispute/Agent aggregates exist in the domain yet — the
    /// repository returns zeros today and will be the single swap point when
    /// the entities are introduced.
    /// </summary>
    public class SupportKpisDto
    {
        // User-requested tracking fields
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int ResolvedTickets { get; set; }

        /// <summary>Human-readable duration, e.g. "4.2h" or "1d 3h".</summary>
        public string AvgResolutionTime { get; set; } = "0h";

        // Secondary tiles the dashboard already uses — kept on the DTO so
        // the UI stays wired without touching the page shape.
        public int UrgentTickets { get; set; }
        public int SlaBreached { get; set; }
        public int DisputesAwaitingReview { get; set; }
        public string AvgFirstResponse { get; set; } = "0h";
        public decimal CsatScore { get; set; }
        public int ResolvedThisMonth { get; set; }
        public int TotalThisMonth { get; set; }
    }
}
