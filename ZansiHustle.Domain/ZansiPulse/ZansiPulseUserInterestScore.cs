using System;
using ZansiHustle.Shared.Enums.ZansiPulse;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// A user's affinity for a (category, optional subcategory) pair. Seeded
    /// from onboarding selections and continuously nudged by behavioural
    /// events. One row per (<see cref="UserId"/>, <see cref="CategoryId"/>,
    /// <see cref="SubCategoryId"/>); a unique index enforces it. Scores are
    /// clamped to a sane range (see <c>ZansiPulseDefaults</c>) so they never
    /// run away. All timestamps are UTC.
    /// </summary>
    public class ZansiPulseUserInterestScore
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }

        public decimal Score { get; set; }

        public ZansiPulseInterestSource Source { get; set; }

        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
