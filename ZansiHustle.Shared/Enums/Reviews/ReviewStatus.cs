namespace ZansiHustle.Shared.Enums.Reviews
{
    /// <summary>
    /// Lifecycle of a Review row. New rows land as <see cref="Active"/>;
    /// owner-initiated deletes flip to <see cref="Deleted"/> (soft
    /// delete) so we keep the row for moderation audit even after the
    /// reviewer removes their public review. <see cref="Hidden"/> is
    /// reserved for future admin moderation.
    /// </summary>
    public enum ReviewStatus
    {
        /// <summary>Visible to the public listing. Counts toward aggregate rating.</summary>
        Active = 1,

        /// <summary>Hidden by admin moderation. Reserved — not used in V1.</summary>
        Hidden = 2,

        /// <summary>Soft-deleted by the reviewer. Excluded from public lists and aggregates.</summary>
        Deleted = 3,
    }
}
