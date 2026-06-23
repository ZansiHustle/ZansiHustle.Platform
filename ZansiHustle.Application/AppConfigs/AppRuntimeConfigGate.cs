using System;
using System.Threading.Tasks;
using ZansiHustle.Application.Persistence.AppConfigs;

namespace ZansiHustle.Application.AppConfigs
{
    public class AppRuntimeConfigGate : IAppRuntimeConfigGate
    {
        private readonly IAppRuntimeConfigRepository _repository;

        public AppRuntimeConfigGate(IAppRuntimeConfigRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> IsEnabledAsync(string key, bool defaultValue = true)
        {
            var config = await _repository.GetByKeyAsync(key);
            if (config is null || !config.IsActive) return defaultValue;
            if (!string.Equals(config.ValueType, "Boolean", StringComparison.OrdinalIgnoreCase))
                return defaultValue;
            return config.BooleanValue;
        }

        public async Task<bool> IsEnabledForUserAsync(string key, string? userEmail, bool defaultValue = true)
        {
            // Normal evaluation first — if the feature is enabled, no override needed.
            if (await IsEnabledAsync(key, defaultValue)) return true;

            // Feature is disabled. The official review test account bypasses runtime
            // gates ONLY when the master switch is explicitly ON (default false here,
            // so a missing/inactive switch never grants a bypass).
            if (AppConfigKeys.IsTestAccount(userEmail) &&
                await IsEnabledAsync(AppConfigKeys.TestAccountFullAccess, false))
            {
                return true;
            }

            return false;
        }
    }
}
