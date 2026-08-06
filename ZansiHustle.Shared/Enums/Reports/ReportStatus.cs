namespace ZansiHustle.Shared.Enums.Reports
{
    /// <summary>Moderation lifecycle of a <c>ContentReport</c>.</summary>
    public enum ReportStatus
    {
        /// <summary>Newly submitted, awaiting moderator triage.</summary>
        Pending = 1,
        /// <summary>A moderator is actively reviewing it.</summary>
        Reviewing = 2,
        /// <summary>Reviewed and enforcement action was taken.</summary>
        ActionTaken = 3,
        /// <summary>Reviewed and no action was warranted.</summary>
        Dismissed = 4
    }
}
