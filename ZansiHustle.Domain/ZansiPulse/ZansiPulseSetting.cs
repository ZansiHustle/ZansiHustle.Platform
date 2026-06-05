using System;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// A single tunable knob for ZansiPulse — event weights, recommendation
    /// blend weights, onboarding seed score, score bounds, etc. — stored as a
    /// string key/value so the intelligence layer can be retuned without a
    /// deploy. Read with a code-default fallback (see <c>ZansiPulseDefaults</c>)
    /// so a missing/disabled row never breaks scoring. One row per
    /// <see cref="Key"/> (unique index). All timestamps are UTC.
    /// </summary>
    public class ZansiPulseSetting
    {
        public Guid Id { get; set; }

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>When false the row is ignored and the code default applies.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
