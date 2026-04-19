using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Media.Storage;

namespace ZansiHustle.API.Storage
{
    /// <summary>
    /// Dev / test / UAT storage adapter.
    ///
    /// Path resolution (first match wins):
    ///   1. Configuration "Media:LocalBasePath" — absolute path the host
    ///      operator explicitly configured. Use this on UAT/Azure App
    ///      Service to point at a guaranteed-writable location
    ///      (e.g. D:\home\data\_media on App Service).
    ///   2. {ContentRootPath}/App_Data/_media — the historical writable
    ///      location under the deployed app. NOT served by the static-file
    ///      pipeline, so private verification blobs aren't accidentally
    ///      exposed.
    ///
    /// Why not wwwroot? It's often made read-only by CI/IIS and is also
    /// served by the static-file middleware — neither property is desirable
    /// for private uploads.
    ///
    /// Signed PUT/GET URLs point at RawMediaController and carry an HMAC
    /// signature over (verb|container|key|expiry) so a GET signature can't
    /// be replayed as a PUT.
    /// </summary>
    public class LocalFilesystemMediaStorageService : IMediaStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _http;
        private readonly IConfiguration _config;
        private readonly ILogger<LocalFilesystemMediaStorageService> _logger;
        private readonly string _hmacKey;

        public LocalFilesystemMediaStorageService(
            IWebHostEnvironment env,
            IHttpContextAccessor http,
            IConfiguration config,
            ILogger<LocalFilesystemMediaStorageService> logger)
        {
            _env = env;
            _http = http;
            _config = config;
            _logger = logger;

            // Prefer a dedicated signing key; fall back to JWT key so dev
            // works out-of-the-box. When neither is set we still have a
            // deterministic fallback so upload tickets always sign.
            _hmacKey = config["Media:SigningKey"]
                       ?? config["JwtSettings:Key"]
                       ?? "zh-local-media-dev-key";

            // Fail fast at startup if the configured media root isn't
            // writable — much easier to diagnose than per-request 500s.
            try
            {
                var root = GetMediaRoot();
                Directory.CreateDirectory(root);
                _logger.LogInformation("[Media] LocalFilesystemMediaStorageService base path: {Path}", root);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Media] Failed to initialise local media base path. Set Media:LocalBasePath in configuration to an absolute, writable directory.");
                // Don't rethrow — we don't want the whole host to fail to
                // start. Per-request calls will surface the concrete error
                // instead (MediaService now wraps them with a clean message).
            }
        }

        public Task<MediaUploadTicket> IssueUploadAsync(
            string container, string storageKey, string contentType, long maxSizeBytes, TimeSpan ttl)
        {
            try
            {
                var dir = Path.Combine(GetMediaRoot(), container, Path.GetDirectoryName(storageKey) ?? string.Empty);
                Directory.CreateDirectory(dir);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "[Media] Cannot create media directory (permission denied). Root: {Root}", GetMediaRoot());
                throw new InvalidOperationException(
                    "Media storage is not writable. Set Media:LocalBasePath to a writable directory.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Media] Failed to prepare media directory.");
                throw;
            }

            var expires = DateTime.UtcNow.Add(ttl);
            var url = BuildSignedUrl(container, storageKey, expires, "PUT");

            var headers = new Dictionary<string, string>
            {
                ["Content-Type"] = contentType,
                ["x-zh-max-size"] = maxSizeBytes.ToString(),
            };

            return Task.FromResult(new MediaUploadTicket
            {
                UploadUrl = url,
                Method = "PUT",
                Headers = headers,
                ExpiresAtUtc = expires,
            });
        }

        public Task<string> IssueReadUrlAsync(string container, string storageKey, TimeSpan ttl)
        {
            var expires = DateTime.UtcNow.Add(ttl);
            return Task.FromResult(BuildSignedUrl(container, storageKey, expires, "GET"));
        }

        public Task<bool> ExistsAsync(string container, string storageKey)
        {
            var path = Path.Combine(GetMediaRoot(), container, storageKey);
            return Task.FromResult(File.Exists(path));
        }

        public Task DeleteAsync(string container, string storageKey)
        {
            var path = Path.Combine(GetMediaRoot(), container, storageKey);
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { _logger.LogWarning(ex, "[Media] Delete failed for {Path}", path); }
            return Task.CompletedTask;
        }

        // ── URL signing ───────────────────────────────────────────────────
        // Verb is part of the signed payload so a GET signature can't be
        // replayed as a PUT. Expiry is unix seconds.
        private string BuildSignedUrl(string container, string storageKey, DateTime expiresUtc, string verb)
        {
            var exp = ((DateTimeOffset)expiresUtc).ToUnixTimeSeconds();
            var sig = Sign($"{verb}|{container}|{storageKey}|{exp}");
            var host = ResolveHost();
            return $"{host}/api/media/raw/{container}/{Uri.EscapeDataString(storageKey)}?sig={sig}&exp={exp}&verb={verb}";
        }

        public bool VerifySignature(string container, string storageKey, string verb, string sig, long exp)
        {
            if (DateTimeOffset.FromUnixTimeSeconds(exp) < DateTimeOffset.UtcNow) return false;
            var expected = Sign($"{verb.ToUpperInvariant()}|{container}|{storageKey}|{exp}");
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(sig));
        }

        private string Sign(string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacKey));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        }

        public string GetAbsolutePath(string container, string storageKey)
            => Path.Combine(GetMediaRoot(), container, storageKey);

        private string GetMediaRoot()
        {
            // 1) Explicit host override — preferred on UAT / Azure App
            //    Service where ContentRoot is read-only but a dedicated
            //    writable path (e.g. D:\home\data\_media) exists.
            var configured = _config["Media:LocalBasePath"];
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.Trim();

            // 2) App_Data under the deployed content root. Convention for
            //    writable, non-served data across Windows/Linux hosts.
            var contentRoot = _env.ContentRootPath;
            if (string.IsNullOrWhiteSpace(contentRoot))
                contentRoot = AppContext.BaseDirectory;
            return Path.Combine(contentRoot, "App_Data", "_media");
        }

        private string ResolveHost()
        {
            var req = _http.HttpContext?.Request;
            if (req is null) return string.Empty;
            return $"{req.Scheme}://{req.Host}";
        }
    }
}
