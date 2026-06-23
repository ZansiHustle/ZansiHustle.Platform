using System;
using System.Linq;

namespace ZansiHustle.Application.AppVersion
{
    /// <summary>
    /// Tiny, dependency-free semantic-version helper for the mobile version gate.
    /// Only the numeric "major.minor.patch" core is compared — any pre-release or
    /// build-metadata suffix (e.g. "1.2.3-rc.1+build") is ignored. Missing minor /
    /// patch components default to 0, so "1" == "1.0.0".
    ///
    /// FAIL-SAFE CONTRACT: an unparseable / empty version returns
    /// <see cref="TryParse"/> == false and the caller MUST treat that as
    /// "do not force an update" — a malformed client header can never lock a user
    /// out of the app.
    /// </summary>
    public static class SemanticVersionComparer
    {
        /// <summary>
        /// Parses the numeric core of a semver string. Returns false (and zeroed
        /// components) when the value is null/blank or the major component is not a
        /// non-negative integer. Never throws.
        /// </summary>
        public static bool TryParse(string? version, out int major, out int minor, out int patch)
        {
            major = 0;
            minor = 0;
            patch = 0;

            if (string.IsNullOrWhiteSpace(version))
                return false;

            // Drop pre-release / build metadata: "1.2.3-rc.1+sha" -> "1.2.3".
            var core = version.Trim();
            var cut = core.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut >= 0)
                core = core.Substring(0, cut);

            var parts = core.Split('.');
            if (parts.Length == 0 || parts.Length > 3)
                return false;

            if (!TryParseComponent(parts.ElementAtOrDefault(0), out major)) return false;
            if (parts.Length >= 2 && !TryParseComponent(parts[1], out minor)) return false;
            if (parts.Length >= 3 && !TryParseComponent(parts[2], out patch)) return false;

            return true;
        }

        private static bool TryParseComponent(string? raw, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            return int.TryParse(raw.Trim(), out value) && value >= 0;
        }

        /// <summary>
        /// Compares two semver strings by numeric core. Returns &lt;0 / 0 / &gt;0
        /// like <see cref="IComparable"/>. When EITHER side is unparseable the
        /// result is 0 (treated as "equal" — i.e. the caller cannot conclude the
        /// installed build is older, so it will not force an update).
        /// </summary>
        public static int Compare(string? left, string? right)
        {
            if (!TryParse(left, out var lMajor, out var lMinor, out var lPatch))
                return 0;
            if (!TryParse(right, out var rMajor, out var rMinor, out var rPatch))
                return 0;

            if (lMajor != rMajor) return lMajor.CompareTo(rMajor);
            if (lMinor != rMinor) return lMinor.CompareTo(rMinor);
            return lPatch.CompareTo(rPatch);
        }

        /// <summary>
        /// True only when <paramref name="installed"/> is BOTH parseable AND
        /// strictly lower than <paramref name="target"/>. A missing/invalid
        /// installed or target version returns false (fail-safe).
        /// </summary>
        public static bool IsLowerThan(string? installed, string? target)
        {
            if (!TryParse(installed, out _, out _, out _)) return false;
            if (!TryParse(target, out _, out _, out _)) return false;
            return Compare(installed, target) < 0;
        }
    }
}
