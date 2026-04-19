using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ZansiHustle.Application.Media.Storage;

namespace ZansiHustle.API.Storage
{
    /// <summary>
    /// Dev / test storage adapter. Files live under
    /// {ContentRoot}/wwwroot/_media/{container}/{storageKey}.
    ///
    /// "Signed" PUT/GET URLs are simply
    ///     {host}/api/media/raw/{container}/{key}?sig={hmac}&exp={unix}
    /// served by RawMediaController. This keeps the upload/download contract
    /// identical to the cloud adapter so the frontend code is portable.
    /// </summary>
    public class LocalFilesystemMediaStorageService : IMediaStorageService
    {
        private readonly IHostEnvironment _env;
        private readonly IHttpContextAccessor _http;
        private readonly string _hmacKey;

        public LocalFilesystemMediaStorageService(
            IHostEnvironment env, IHttpContextAccessor http, IConfiguration config)
        {
            _env = env;
            _http = http;
            // Reuses the JWT signing key as a dev-only HMAC for URL signing.
            // Replace with a dedicated key for any non-dev deployment that
            // still uses the local adapter.
            _hmacKey = config["JwtSettings:Key"] ?? "zh-local-media-dev-key";
        }

        public Task<MediaUploadTicket> IssueUploadAsync(
            string container, string storageKey, string contentType, long maxSizeBytes, TimeSpan ttl)
        {
            // Make sure the directory exists ahead of the upload. The signed
            // PUT route writes into this path verbatim.
            var dir = Path.Combine(GetMediaRoot(), container, Path.GetDirectoryName(storageKey) ?? "");
            Directory.CreateDirectory(dir);

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
            try { if (File.Exists(path)) File.Delete(path); } catch { /* swallow */ }
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
            var web = (_env as IWebHostEnvironment)?.WebRootPath
                       ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            return Path.Combine(web, "_media");
        }

        private string ResolveHost()
        {
            var req = _http.HttpContext?.Request;
            if (req is null) return "";
            return $"{req.Scheme}://{req.Host}";
        }
    }
}
