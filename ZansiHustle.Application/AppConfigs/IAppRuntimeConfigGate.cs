using System.Threading.Tasks;

namespace ZansiHustle.Application.AppConfigs
{
    /// <summary>
    /// Lightweight server-side check of a boolean runtime config / feature flag.
    /// FAIL-OPEN: a missing/inactive/non-boolean row resolves to
    /// <paramref name="defaultValue"/> (default true), so a config gap never
    /// blocks a flow — only an explicit BooleanValue=false disables it.
    /// </summary>
    public interface IAppRuntimeConfigGate
    {
        Task<bool> IsEnabledAsync(string key, bool defaultValue = true);

        /// <summary>
        /// Like <see cref="IsEnabledAsync"/>, but grants the official app-review
        /// test account (<see cref="AppConfigKeys.TestAccountEmail"/>) a bypass:
        /// when <c>testAccountAccessAllEnabled</c> is on and the caller is that
        /// exact account, every runtime feature gate is treated as enabled.
        /// Normal users are unaffected. Bypasses ONLY runtime feature gates —
        /// never auth, ownership, stock, amount, or other business validation.
        /// </summary>
        Task<bool> IsEnabledForUserAsync(string key, string? userEmail, bool defaultValue = true);
    }

    /// <summary>Well-known keys / values for runtime config gating.</summary>
    public static class AppConfigKeys
    {
        public const string TestAccountFullAccess = "testAccountAccessAllEnabled";

        /// <summary>The ONLY account allowed to bypass runtime gates. Exact match.</summary>
        public const string TestAccountEmail = "test@zansihustle.co.za";

        public static bool IsTestAccount(string? email) =>
            !string.IsNullOrWhiteSpace(email) &&
            string.Equals(email.Trim(), TestAccountEmail, System.StringComparison.OrdinalIgnoreCase);
    }
}
