using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.AppVersion;
using ZansiHustle.Application.AppVersion.Dtos;
using ZansiHustle.Domain.AppVersion;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.AppVersion;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.AppVersion
{
    /// <summary>
    /// Default <see cref="IMobileAppVersionService"/> implementation. Talks to
    /// <see cref="AppDbContext"/> directly (same pattern as ChatService /
    /// AgentPayoutService — no repository indirection for a single small table).
    ///
    /// Resolution order for a check (first match wins):
    ///   1. enabled rule for the exact (Platform, Channel),
    ///   2. enabled rule for (Platform, Generic),
    ///   3. the appsettings "MobileAppVersion" fallback options.
    ///
    /// Decision (per the portal+mobile contract):
    ///   • build comparison wins ONLY when BOTH the installed build AND the
    ///     minimum/latest build are valid (&gt; 0); otherwise semver is used.
    ///   • a missing/invalid installed version or build NEVER forces an update.
    ///   • updateRequired = rule.UpdateRequired OR installed &lt; minimum.
    ///   • updateAvailable = rule.UpdateAvailable OR installed &lt; latest.
    /// </summary>
    public sealed class MobileAppVersionService : IMobileAppVersionService
    {
        private readonly AppDbContext _db;
        private readonly MobileAppVersionFallbackOptions _fallback;

        public MobileAppVersionService(
            AppDbContext db,
            IOptions<MobileAppVersionFallbackOptions> fallback)
        {
            _db = db;
            _fallback = fallback.Value;
        }

        public async Task<MobileAppVersionCheckResponseDto> CheckAsync(
            MobileAppPlatform platform,
            MobileAppChannel channel,
            string? installedVersion,
            int? installedBuildNumber,
            CancellationToken cancellationToken = default)
        {
            var resolved = await ResolveAsync(platform, channel, cancellationToken);
            return Evaluate(resolved, installedVersion, installedBuildNumber);
        }

        /// <summary>
        /// Pure decision logic, shared by the public endpoint and the gate
        /// middleware. <paramref name="r"/> is the effective rule (DB or fallback).
        /// </summary>
        private static MobileAppVersionCheckResponseDto Evaluate(
            EffectiveRule r,
            string? installedVersion,
            int? installedBuildNumber)
        {
            var installedBuild = installedBuildNumber ?? 0;

            // Build comparison applies ONLY when both sides are valid (> 0).
            var canCompareMinBuild = installedBuild > 0 && r.MinimumSupportedBuildNumber > 0;
            var canCompareLatestBuild = installedBuild > 0 && r.LatestBuildNumber > 0;

            // ── below minimum (forces update when true) ──────────────────────
            bool belowMinimum;
            if (canCompareMinBuild)
                belowMinimum = installedBuild < r.MinimumSupportedBuildNumber;
            else
                // Fail-safe: only forces when the installed version actually parses
                // AND is strictly lower than the configured minimum.
                belowMinimum = SemanticVersionComparer.IsLowerThan(installedVersion, r.MinimumSupportedVersion);

            // ── below latest (soft "update available") ───────────────────────
            bool belowLatest;
            if (canCompareLatestBuild)
                belowLatest = installedBuild < r.LatestBuildNumber;
            else
                belowLatest = SemanticVersionComparer.IsLowerThan(installedVersion, r.LatestVersion);

            var updateRequired = r.UpdateRequired || belowMinimum;
            var updateAvailable = r.UpdateAvailable || belowLatest || updateRequired;

            return new MobileAppVersionCheckResponseDto
            {
                UpdateRequired = updateRequired,
                UpdateAvailable = updateAvailable,
                Title = r.Title,
                Message = r.Message,
                PrimaryButtonText = r.PrimaryButtonText,
                SecondaryButtonText = r.SecondaryButtonText,
                StoreUrl = r.StoreUrl,
                ReleaseNotes = r.ReleaseNotes,
                LatestVersion = r.LatestVersion,
                LatestBuildNumber = r.LatestBuildNumber,
                MinimumSupportedVersion = r.MinimumSupportedVersion,
                MinimumSupportedBuildNumber = r.MinimumSupportedBuildNumber
            };
        }

        /// <summary>Resolves the effective rule (exact → generic → fallback options).</summary>
        private async Task<EffectiveRule> ResolveAsync(
            MobileAppPlatform platform,
            MobileAppChannel channel,
            CancellationToken cancellationToken)
        {
            var exact = await _db.MobileAppVersionRules
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.IsEnabled && x.Platform == platform && x.Channel == channel,
                    cancellationToken);
            if (exact != null)
                return EffectiveRule.FromRule(exact);

            if (channel != MobileAppChannel.Generic)
            {
                var generic = await _db.MobileAppVersionRules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.IsEnabled && x.Platform == platform && x.Channel == MobileAppChannel.Generic,
                        cancellationToken);
                if (generic != null)
                    return EffectiveRule.FromRule(generic);
            }

            return EffectiveRule.FromFallback(_fallback);
        }

        // ── Admin CRUD ─────────────────────────────────────────────────────────

        public async Task<Result<List<MobileAppVersionRuleDto>>> GetRulesAsync(CancellationToken cancellationToken = default)
        {
            var rules = await _db.MobileAppVersionRules
                .AsNoTracking()
                .OrderBy(x => x.Platform)
                .ThenBy(x => x.Channel)
                .ToListAsync(cancellationToken);

            return Result<List<MobileAppVersionRuleDto>>.Success(rules.Select(MapToDto).ToList());
        }

        public async Task<Result<MobileAppVersionRuleDto>> GetRuleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var rule = await _db.MobileAppVersionRules
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            if (rule == null)
                return Result<MobileAppVersionRuleDto>.Failure("NOT_FOUND", "Version rule not found.");

            return Result<MobileAppVersionRuleDto>.Success(MapToDto(rule));
        }

        public async Task<Result<MobileAppVersionRuleDto>> CreateRuleAsync(
            UpsertMobileAppVersionRuleRequestDto request,
            Guid? actingUserId,
            CancellationToken cancellationToken = default)
        {
            var validation = Validate(request);
            if (validation != null)
                return Result<MobileAppVersionRuleDto>.Failure("BAD_REQUEST", validation);

            var duplicate = await _db.MobileAppVersionRules.AnyAsync(
                x => x.Platform == request.Platform && x.Channel == request.Channel,
                cancellationToken);
            if (duplicate)
                return Result<MobileAppVersionRuleDto>.Failure(
                    "CONFLICT",
                    "A rule for this platform and channel already exists.");

            var now = DateTime.UtcNow;
            var rule = new MobileAppVersionRule
            {
                Id = Guid.NewGuid(),
                Platform = request.Platform,
                Channel = request.Channel,
                CreatedAtUtc = now,
            };
            ApplyRequest(rule, request, actingUserId, now);

            _db.MobileAppVersionRules.Add(rule);
            await _db.SaveChangesAsync(cancellationToken);

            return Result<MobileAppVersionRuleDto>.Success(MapToDto(rule), "Version rule created.");
        }

        public async Task<Result<MobileAppVersionRuleDto>> UpdateRuleAsync(
            Guid id,
            UpsertMobileAppVersionRuleRequestDto request,
            Guid? actingUserId,
            CancellationToken cancellationToken = default)
        {
            var validation = Validate(request);
            if (validation != null)
                return Result<MobileAppVersionRuleDto>.Failure("BAD_REQUEST", validation);

            var rule = await _db.MobileAppVersionRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (rule == null)
                return Result<MobileAppVersionRuleDto>.Failure("NOT_FOUND", "Version rule not found.");

            // (Platform, Channel) is the unique business key — block moving a rule
            // onto a target another row already owns.
            var duplicate = await _db.MobileAppVersionRules.AnyAsync(
                x => x.Id != id && x.Platform == request.Platform && x.Channel == request.Channel,
                cancellationToken);
            if (duplicate)
                return Result<MobileAppVersionRuleDto>.Failure(
                    "CONFLICT",
                    "A rule for this platform and channel already exists.");

            rule.Platform = request.Platform;
            rule.Channel = request.Channel;
            ApplyRequest(rule, request, actingUserId, DateTime.UtcNow);

            await _db.SaveChangesAsync(cancellationToken);

            return Result<MobileAppVersionRuleDto>.Success(MapToDto(rule), "Version rule updated.");
        }

        public async Task<Result<MobileAppVersionRuleDto>> SetRuleEnabledAsync(
            Guid id,
            bool isEnabled,
            Guid? actingUserId,
            CancellationToken cancellationToken = default)
        {
            var rule = await _db.MobileAppVersionRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (rule == null)
                return Result<MobileAppVersionRuleDto>.Failure("NOT_FOUND", "Version rule not found.");

            rule.IsEnabled = isEnabled;
            rule.UpdatedByUserId = actingUserId;
            rule.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return Result<MobileAppVersionRuleDto>.Success(
                MapToDto(rule),
                isEnabled ? "Version rule activated." : "Version rule deactivated.");
        }

        // ── helpers ──────────────────────────────────────────────────────────

        /// <summary>Returns a validation error message, or null when valid.</summary>
        private static string? Validate(UpsertMobileAppVersionRuleRequestDto request)
        {
            if (!SemanticVersionComparer.TryParse(request.LatestVersion, out _, out _, out _))
                return "LatestVersion must be a valid semantic version (e.g. 1.0.0).";
            if (!SemanticVersionComparer.TryParse(request.MinimumSupportedVersion, out _, out _, out _))
                return "MinimumSupportedVersion must be a valid semantic version (e.g. 1.0.0).";
            if (request.LatestBuildNumber < 0)
                return "LatestBuildNumber must be zero or greater.";
            if (request.MinimumSupportedBuildNumber < 0)
                return "MinimumSupportedBuildNumber must be zero or greater.";
            if (request.UpdateRequired && string.IsNullOrWhiteSpace(request.StoreUrl))
                return "StoreUrl is required when UpdateRequired is true.";
            return null;
        }

        private static void ApplyRequest(
            MobileAppVersionRule rule,
            UpsertMobileAppVersionRuleRequestDto request,
            Guid? actingUserId,
            DateTime nowUtc)
        {
            rule.LatestVersion = request.LatestVersion.Trim();
            rule.LatestBuildNumber = request.LatestBuildNumber;
            rule.MinimumSupportedVersion = request.MinimumSupportedVersion.Trim();
            rule.MinimumSupportedBuildNumber = request.MinimumSupportedBuildNumber;
            rule.UpdateRequired = request.UpdateRequired;
            rule.UpdateAvailable = request.UpdateAvailable;
            rule.IsEnabled = request.IsEnabled;
            rule.Title = request.Title ?? string.Empty;
            rule.Message = request.Message ?? string.Empty;
            rule.PrimaryButtonText = request.PrimaryButtonText ?? string.Empty;
            rule.SecondaryButtonText = request.SecondaryButtonText ?? string.Empty;
            rule.StoreUrl = request.StoreUrl ?? string.Empty;
            rule.ReleaseNotes = request.ReleaseNotes;
            rule.UpdatedByUserId = actingUserId;
            rule.UpdatedAtUtc = nowUtc;
        }

        private static MobileAppVersionRuleDto MapToDto(MobileAppVersionRule rule) => new()
        {
            Id = rule.Id,
            Platform = rule.Platform,
            Channel = rule.Channel,
            LatestVersion = rule.LatestVersion,
            LatestBuildNumber = rule.LatestBuildNumber,
            MinimumSupportedVersion = rule.MinimumSupportedVersion,
            MinimumSupportedBuildNumber = rule.MinimumSupportedBuildNumber,
            UpdateRequired = rule.UpdateRequired,
            UpdateAvailable = rule.UpdateAvailable,
            IsEnabled = rule.IsEnabled,
            Title = rule.Title,
            Message = rule.Message,
            PrimaryButtonText = rule.PrimaryButtonText,
            SecondaryButtonText = rule.SecondaryButtonText,
            StoreUrl = rule.StoreUrl,
            ReleaseNotes = rule.ReleaseNotes,
            UpdatedByUserId = rule.UpdatedByUserId,
            CreatedAtUtc = rule.CreatedAtUtc,
            UpdatedAtUtc = rule.UpdatedAtUtc,
            RowVersion = rule.RowVersion is { Length: > 0 } ? Convert.ToBase64String(rule.RowVersion) : null,
        };

        /// <summary>
        /// The merged set of fields the decision logic needs — sourced either from
        /// a DB rule or the appsettings fallback, so <see cref="Evaluate"/> never
        /// branches on origin.
        /// </summary>
        private sealed class EffectiveRule
        {
            public string LatestVersion { get; init; } = string.Empty;
            public int LatestBuildNumber { get; init; }
            public string MinimumSupportedVersion { get; init; } = string.Empty;
            public int MinimumSupportedBuildNumber { get; init; }
            public bool UpdateRequired { get; init; }
            public bool UpdateAvailable { get; init; }
            public string Title { get; init; } = string.Empty;
            public string Message { get; init; } = string.Empty;
            public string PrimaryButtonText { get; init; } = string.Empty;
            public string SecondaryButtonText { get; init; } = string.Empty;
            public string StoreUrl { get; init; } = string.Empty;
            public string? ReleaseNotes { get; init; }

            public static EffectiveRule FromRule(MobileAppVersionRule r) => new()
            {
                LatestVersion = r.LatestVersion,
                LatestBuildNumber = r.LatestBuildNumber,
                MinimumSupportedVersion = r.MinimumSupportedVersion,
                MinimumSupportedBuildNumber = r.MinimumSupportedBuildNumber,
                UpdateRequired = r.UpdateRequired,
                UpdateAvailable = r.UpdateAvailable,
                Title = r.Title,
                Message = r.Message,
                PrimaryButtonText = r.PrimaryButtonText,
                SecondaryButtonText = r.SecondaryButtonText,
                StoreUrl = r.StoreUrl,
                ReleaseNotes = r.ReleaseNotes,
            };

            public static EffectiveRule FromFallback(MobileAppVersionFallbackOptions o) => new()
            {
                LatestVersion = o.LatestVersion,
                LatestBuildNumber = o.LatestBuildNumber,
                MinimumSupportedVersion = o.MinimumSupportedVersion,
                MinimumSupportedBuildNumber = o.MinimumSupportedBuildNumber,
                UpdateRequired = o.UpdateRequired,
                UpdateAvailable = o.UpdateAvailable,
                Title = o.Title,
                Message = o.Message,
                PrimaryButtonText = o.PrimaryButtonText,
                SecondaryButtonText = o.SecondaryButtonText,
                StoreUrl = o.StoreUrl,
                ReleaseNotes = o.ReleaseNotes,
            };
        }
    }
}
