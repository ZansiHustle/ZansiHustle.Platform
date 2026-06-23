using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.AppVersion.Dtos;
using ZansiHustle.Shared.Enums.AppVersion;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.AppVersion
{
    /// <summary>
    /// Resolves the mobile version-check answer (public endpoint + gate middleware)
    /// and drives the admin CRUD over <c>MobileAppVersionRule</c> rows. The check
    /// is fail-safe: a missing/invalid installed version or build NEVER forces an
    /// update, and an absent rule falls back to Platform+Generic, then to the
    /// appsettings "MobileAppVersion" section.
    /// </summary>
    public interface IMobileAppVersionService
    {
        /// <summary>
        /// Computes the update decision for an installed build. <paramref name="installedVersion"/>
        /// / <paramref name="installedBuildNumber"/> are the client's reported values
        /// (null/blank/&lt;=0 when unknown). Always returns a populated response —
        /// the fallback guarantees a safe answer even before any DB row exists.
        /// </summary>
        Task<MobileAppVersionCheckResponseDto> CheckAsync(
            MobileAppPlatform platform,
            MobileAppChannel channel,
            string? installedVersion,
            int? installedBuildNumber,
            CancellationToken cancellationToken = default);

        Task<Result<List<MobileAppVersionRuleDto>>> GetRulesAsync(CancellationToken cancellationToken = default);

        Task<Result<MobileAppVersionRuleDto>> GetRuleAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Result<MobileAppVersionRuleDto>> CreateRuleAsync(
            UpsertMobileAppVersionRuleRequestDto request,
            Guid? actingUserId,
            CancellationToken cancellationToken = default);

        Task<Result<MobileAppVersionRuleDto>> UpdateRuleAsync(
            Guid id,
            UpsertMobileAppVersionRuleRequestDto request,
            Guid? actingUserId,
            CancellationToken cancellationToken = default);

        /// <summary>Enables or disables a rule (no hard delete). <paramref name="isEnabled"/> sets the target state.</summary>
        Task<Result<MobileAppVersionRuleDto>> SetRuleEnabledAsync(
            Guid id,
            bool isEnabled,
            Guid? actingUserId,
            CancellationToken cancellationToken = default);
    }
}
