using System;

namespace ZansiHustle.Domain.ZansiDispatch
{
    /// <summary>
    /// A tunable ZansiDispatch knob — quote fees, buffers, expiry, feature
    /// flags — stored as a string key/value so logistics can be retuned without
    /// a deploy. Read with a code-default fallback (see
    /// <c>ZansiDispatchDefaults</c>) so a missing/disabled row never breaks
    /// quoting. One row per <see cref="Key"/> (unique index). UTC timestamps.
    /// </summary>
    public class ZansiDispatchSetting
    {
        public Guid Id { get; set; }

        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>When false the row is ignored and the code default applies.</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
