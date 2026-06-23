using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ZansiHustle.Application.AppVersion;

namespace ZansiHustle.API.Middleware
{
    /// <summary>
    /// Hard-update gate for the mobile app. Reads the X-App-* request headers and,
    /// when the installed build is forced-outdated for its (Platform, Channel),
    /// short-circuits with HTTP 426 and a raw JSON body the mobile client knows
    /// how to render.
    ///
    /// SAFETY RULES (must never block the wrong caller):
    ///   • If X-App-Version OR X-App-Build is absent → ALLOW. Web/portal and any
    ///     header-less caller pass straight through.
    ///   • Soft "update available" NEVER blocks — only a hard updateRequired does.
    ///   • Exempt paths: /api/app-version (no self-block / loop), the real auth
    ///     login/register/refresh routes, health/status, and any non-/api path.
    ///   • The version check is fail-safe (missing/invalid version or build never
    ///     forces an update), so a malformed header can never lock a user out.
    /// </summary>
    public sealed class AppVersionGateMiddleware
    {
        private readonly RequestDelegate _next;

        // 426 Upgrade Required — the contract body mobile expects.
        private const string OutdatedCode = "OUTDATED_APP";
        private const string OutdatedMessage = "Your app is outdated. Please update to continue.";
        private const string OutdatedTitle = "Update required";
        private const string OutdatedPrimaryButton = "Update now";

        public AppVersionGateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IMobileAppVersionService service)
        {
            var path = context.Request.Path;

            if (IsExempt(path))
            {
                await _next(context);
                return;
            }

            var version = context.Request.Headers["X-App-Version"].ToString();
            var build = context.Request.Headers["X-App-Build"].ToString();

            // Header-less callers (web, portal, server-to-server) are never gated.
            if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(build))
            {
                await _next(context);
                return;
            }

            var platform = MobileAppWireParser.ParsePlatform(context.Request.Headers["X-App-Platform"].ToString());
            var channel = MobileAppWireParser.ParseChannel(context.Request.Headers["X-App-Channel"].ToString());
            var buildNumber = MobileAppWireParser.ParseBuildNumber(build);

            var check = await service.CheckAsync(
                platform, channel, version, buildNumber, context.RequestAborted);

            // Soft updates never block; only a hard required update does.
            if (!check.UpdateRequired)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.UpgradeRequired; // 426
            context.Response.ContentType = "application/json";

            var payload = new
            {
                code = OutdatedCode,
                message = OutdatedMessage,
                title = OutdatedTitle,
                storeUrl = check.StoreUrl ?? string.Empty,
                primaryButtonText = OutdatedPrimaryButton
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload), context.RequestAborted);
        }

        /// <summary>
        /// True for paths that must never be gated: the version endpoint itself
        /// (prevents a self-block / loop), the auth bootstrap routes (login /
        /// register / refresh), health/status probes, and anything outside /api.
        /// </summary>
        private static bool IsExempt(PathString path)
        {
            // Any non-/api path (web assets, SignalR hubs, swagger, root, etc.).
            if (!path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                return true;

            // The version-check endpoint must always be reachable.
            if (path.StartsWithSegments("/api/app-version", StringComparison.OrdinalIgnoreCase))
                return true;

            // Auth bootstrap — never lock a user out of getting a session/token.
            if (path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/api/auth/register", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
                return true;

            // Health / status probes.
            if (path.StartsWithSegments("/api/health", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWithSegments("/api/status", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}
