using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Infrastructure.Configuration;

namespace ZansiHustle.Infrastructure.Payments.Yoco
{
    /// <summary>
    /// Standard Webhooks signature verification for Yoco. See
    /// <see cref="IYocoSignatureService"/> for the spec.
    ///
    /// This is intentionally narrow: HMAC-SHA256 over the well-defined
    /// "{id}.{timestamp}.{body}" content, constant-time compared. The
    /// implementation is straightforward and matches Yoco's developer
    /// docs — unlike the Ozow hash spec, there is no field-ordering
    /// guesswork involved.
    /// </summary>
    public sealed class YocoSignatureService : IYocoSignatureService
    {
        private const string SecretPrefix = "whsec_";

        // Replay-protection window. Yoco's spec recommends rejecting events
        // older than ~5 minutes to prevent captured-and-replayed webhooks.
        private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromMinutes(5);

        private readonly YocoSettings _settings;
        private readonly ILogger<YocoSignatureService> _logger;

        public YocoSignatureService(IOptions<YocoSettings> settings, ILogger<YocoSignatureService> logger)
        {
            _settings = settings.Value ?? new YocoSettings();
            _logger = logger;
        }

        public bool IsImplemented => !string.IsNullOrWhiteSpace(_settings.WebhookSigningSecret);

        public bool VerifyWebhookSignature(string rawBody, string? webhookId, string? webhookTimestamp, string? webhookSignature)
        {
            if (!IsImplemented)
            {
                _logger.LogWarning("[Yoco] VerifyWebhookSignature called but WebhookSigningSecret is not configured.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(webhookId)
                || string.IsNullOrWhiteSpace(webhookTimestamp)
                || string.IsNullOrWhiteSpace(webhookSignature)
                || rawBody is null)
            {
                _logger.LogWarning(
                    "[Yoco] Missing webhook header(s). id={HasId} ts={HasTs} sig={HasSig} body={HasBody}",
                    !string.IsNullOrWhiteSpace(webhookId),
                    !string.IsNullOrWhiteSpace(webhookTimestamp),
                    !string.IsNullOrWhiteSpace(webhookSignature),
                    rawBody is not null);
                return false;
            }

            // Replay-window check. Timestamp is unix seconds.
            if (!long.TryParse(webhookTimestamp, out var unixSeconds))
            {
                _logger.LogWarning("[Yoco] webhook-timestamp '{Ts}' is not a valid unix-seconds value.", webhookTimestamp);
                return false;
            }

            var sentAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            var skew = DateTimeOffset.UtcNow - sentAt;
            if (skew > AllowedClockSkew || skew < -AllowedClockSkew)
            {
                _logger.LogWarning(
                    "[Yoco] webhook-timestamp outside the {Window}-minute replay window (skew={SkewSeconds}s).",
                    AllowedClockSkew.TotalMinutes, (long)skew.TotalSeconds);
                return false;
            }

            byte[] secretBytes;
            try
            {
                var raw = _settings.WebhookSigningSecret.StartsWith(SecretPrefix, StringComparison.Ordinal)
                    ? _settings.WebhookSigningSecret.Substring(SecretPrefix.Length)
                    : _settings.WebhookSigningSecret;
                secretBytes = Convert.FromBase64String(raw);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "[Yoco] WebhookSigningSecret is not valid base64.");
                return false;
            }

            var signedContent = $"{webhookId}.{webhookTimestamp}.{rawBody}";
            byte[] computed;
            try
            {
                using var hmac = new HMACSHA256(secretBytes);
                computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Yoco] HMAC computation threw.");
                return false;
            }

            var computedB64 = Convert.ToBase64String(computed);

            // The header may contain multiple space-separated entries:
            //   "v1,<sigA> v1,<sigB>"
            // Accept any match.
            foreach (var entry in webhookSignature.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = entry.Trim();
                var commaIdx = trimmed.IndexOf(',');
                if (commaIdx <= 0 || commaIdx == trimmed.Length - 1) continue;

                var version = trimmed.Substring(0, commaIdx);
                var sig = trimmed.Substring(commaIdx + 1);

                // Only v1 is defined today.
                if (!string.Equals(version, "v1", StringComparison.Ordinal)) continue;

                if (FixedTimeBase64Equals(computedB64, sig)) return true;
            }

            _logger.LogWarning("[Yoco] No webhook signature matched. id={Id}", webhookId);
            return false;
        }

        private static bool FixedTimeBase64Equals(string a, string b)
        {
            if (a is null || b is null) return false;
            if (a.Length != b.Length) return false;

            // Compare the UTF-8 bytes in constant time to avoid timing attacks.
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(a),
                Encoding.UTF8.GetBytes(b));
        }
    }
}
