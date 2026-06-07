using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Infrastructure.Configuration;

namespace ZansiHustle.Infrastructure.Payments.Ozow
{
    /// <summary>
    /// SHA512 hash service for Ozow.
    ///
    /// Spec source: Ozow developer documentation,
    /// "How to generate the hashCheck" + "Validating the response hash"
    /// sections (Post Payment Request / Transaction Notification Response).
    ///
    /// Both directions follow the same recipe:
    ///   1. Concatenate the canonical fields (no separators) in the documented
    ///      order, with the merchant <c>PrivateKey</c> appended as the trailing
    ///      salt.
    ///   2. ToLowerInvariant the whole string.
    ///   3. UTF-8 encode → SHA512 → hex (lowercase) → that is the hash.
    ///
    /// Verification uses <see cref="CryptographicOperations.FixedTimeEquals"/>
    /// to avoid leaking timing information about hash mismatches.
    ///
    /// Operational notes:
    ///   • PrivateKey never appears in logs or exceptions. Errors only
    ///     reference the TransactionReference / Status — the caller-side
    ///     identifiers, never the salt.
    ///   • Amounts are formatted using <c>InvariantCulture</c> with two
    ///     decimal places ("0.00"). Ozow rejects locale-formatted strings.
    ///   • IsTest is serialised as the lowercase literal "true"/"false"
    ///     (matching the JSON wire form), because Ozow lowercases the whole
    ///     concatenated input before hashing — using "True"/"False" would
    ///     still hash to the same value, but lowercase is the unambiguous
    ///     form documented by Ozow.
    /// </summary>
    public sealed class OzowHashService : IOzowHashService
    {
        private readonly OzowSettings _settings;
        private readonly ILogger<OzowHashService> _logger;

        public OzowHashService(IOptions<OzowSettings> settings, ILogger<OzowHashService> logger)
        {
            _settings = settings.Value ?? new OzowSettings();
            _logger = logger;
        }

        /// <summary>
        /// The service is "implemented" once a non-empty PrivateKey is bound.
        /// Without it we cannot compute a hash Ozow will accept, so refusing
        /// up-front prevents both the deploy-time PostPaymentRequest failure
        /// and the worse case of accepting a forged webhook.
        /// </summary>
        public bool IsImplemented => !string.IsNullOrWhiteSpace(_settings.PrivateKey);

        /// <inheritdoc />
        public string GenerateRequestHash(OzowPaymentRequest request)
            => GenerateRequestHashWithDiagnostic(request).Hash;

        /// <inheritdoc />
        public (string Hash, OzowHashDiagnostic Diagnostic) GenerateRequestHashWithDiagnostic(OzowPaymentRequest request)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            if (!IsImplemented)
            {
                // Defence-in-depth — callers must already be guarding with
                // IsImplemented, but if they didn't, refuse rather than emit
                // a hash that uses an empty salt.
                _logger.LogError(
                    "OzowHashService.GenerateRequestHash called with no PrivateKey configured. " +
                    "Set the Ozow__PrivateKey env var on the host.");
                throw new InvalidOperationException(
                    "Ozow PrivateKey is not configured. Set Ozow__PrivateKey before initiating payments.");
            }

            // ── Trim PrivateKey ─────────────────────────────────────────────
            // Trailing newlines or spaces from copy-pasting an env var value
            // into a hosting-platform secrets UI are a frequent cause of
            // "HashCheck value has failed". Trim BOTH ways: log when it
            // changed so an operator sees the lurking config bug, but use
            // the trimmed value for the hash so it actually works.
            var rawPk = _settings.PrivateKey ?? string.Empty;
            var trimmedPk = rawPk.Trim();
            var pkTrimChanged = !string.Equals(rawPk, trimmedPk, StringComparison.Ordinal);
            if (pkTrimChanged)
            {
                _logger.LogWarning(
                    "[Ozow][Hash] PrivateKey had surrounding whitespace (raw length={Raw}, trimmed length={Trimmed}). " +
                    "Trimmed for hash. Fix the env var to remove trailing whitespace.",
                    rawPk.Length, trimmedPk.Length);
            }

            // Canonical field order. Per Ozow's current PostPaymentRequest
            // specification (docs.ozow.com), Optional1..5 + Customer ARE
            // included in the hash AFTER BankReference and BEFORE the URLs.
            // Older docs / community SDKs omit them — that's the bug here:
            // we send `optional1: <orderId>` on the wire but didn't include
            // it in the hash, so Ozow's server-side recompute saw a
            // different string and rejected the request.
            //
            //   SiteCode
            //   CountryCode
            //   CurrencyCode
            //   Amount               (decimal as "0.00", invariant culture)
            //   TransactionReference
            //   BankReference
            //   Optional1            (empty string when null)
            //   Optional2
            //   Optional3
            //   Optional4
            //   Optional5
            //   Customer
            //   CancelUrl
            //   ErrorUrl
            //   SuccessUrl
            //   NotifyUrl
            //   IsTest               ("true" / "false")
            //   PrivateKey           (trailing salt — never sent on the wire)
            //
            // Empty optional fields are concatenated as "" — the canonical
            // form, matching what Ozow's server does when recomputing.
            var fields = new List<OzowHashField>
            {
                Field("SiteCode",             request.SiteCode),
                Field("CountryCode",          request.CountryCode),
                Field("CurrencyCode",         request.CurrencyCode),
                AmountField(request.Amount),
                Field("TransactionReference", request.TransactionReference),
                Field("BankReference",        request.BankReference),
                Field("Optional1",            request.Optional1),
                Field("Optional2",            request.Optional2),
                Field("Optional3",            request.Optional3),
                Field("Optional4",            request.Optional4),
                Field("Optional5",            request.Optional5),
                Field("Customer",             request.Customer),
                Field("CancelUrl",            request.CancelUrl),
                Field("ErrorUrl",             request.ErrorUrl),
                Field("SuccessUrl",           request.SuccessUrl),
                Field("NotifyUrl",            request.NotifyUrl),
                IsTestField(request.IsTest),
            };

            var sb = new StringBuilder(1024);
            foreach (var f in fields) sb.Append(f.HashValue);
            sb.Append(trimmedPk);

            var hash = Sha512HexLower(sb.ToString());

            // Whitespace-changed checks on the public-ish fields (never on
            // PrivateKey value). Helps operators spot bad env vars without
            // recompiling.
            var siteCodeTrimChanged =
                !string.Equals(request.SiteCode ?? string.Empty, (request.SiteCode ?? string.Empty).Trim(), StringComparison.Ordinal);
            var anyUrlTrimChanged =
                UrlTrimChanged(request.CancelUrl)
                || UrlTrimChanged(request.ErrorUrl)
                || UrlTrimChanged(request.SuccessUrl)
                || UrlTrimChanged(request.NotifyUrl);

            // Body-vs-hash mismatch detection. With the new canonical hash
            // we wrote the hash input directly off the request DTO, so each
            // OzowHashField has BodyValue == HashValue. If this flag ever
            // flips true post-deploy, a future change introduced a side-of-
            // hashing transformation and the caller's log will name it.
            var anyMismatch = fields.Exists(f => !string.Equals(f.BodyValue, f.HashValue, StringComparison.Ordinal));

            var diagnostic = new OzowHashDiagnostic
            {
                Fields = fields,
                PrivateKeyLength = trimmedPk.Length,
                PrivateKeyTrimChanged = pkTrimChanged,
                SiteCodeTrimChanged = siteCodeTrimChanged,
                AnyUrlTrimChanged = anyUrlTrimChanged,
                AnyMismatch = anyMismatch,
            };

            return (hash, diagnostic);
        }

        // Field helper: body-value form == hash-input form. Null becomes "".
        private static OzowHashField Field(string name, string? value)
        {
            var v = value ?? string.Empty;
            return new OzowHashField(name, v, v);
        }

        // Amount renders identically in hash + body — "0.00" invariant.
        private static OzowHashField AmountField(decimal amount)
        {
            var v = amount.ToString("0.00", CultureInfo.InvariantCulture);
            return new OzowHashField("Amount", v, v);
        }

        // bool → "true"/"false" lowercase in both hash + body.
        private static OzowHashField IsTestField(bool isTest)
        {
            var v = isTest ? "true" : "false";
            return new OzowHashField("IsTest", v, v);
        }

        private static bool UrlTrimChanged(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return !string.Equals(url, url.Trim(), StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public bool ValidateNotificationHash(OzowTransactionNotification notification)
        {
            if (notification is null) return false;

            if (!IsImplemented)
            {
                // Refuse without a configured salt — the only "valid" hash in
                // that state would be one computed against an empty key, which
                // anyone can produce. Park the event for ops review instead.
                _logger.LogWarning(
                    "OzowHashService.ValidateNotificationHash called with no PrivateKey configured. " +
                    "Returning false for TransactionReference={Ref}.",
                    notification.TransactionReference);
                return false;
            }

            var providedHash = notification.Hash;
            if (string.IsNullOrWhiteSpace(providedHash))
            {
                _logger.LogWarning(
                    "Ozow notification arrived with no Hash field. Ref={Ref} Status={Status}",
                    notification.TransactionReference, notification.Status);
                return false;
            }

            // Canonical field order for the notification hash. Copied directly
            // from Ozow's "Validating the response hash" section. Note that
            // every Optional1..5 slot participates, even when null/empty.
            //
            //   SiteCode
            //   TransactionId
            //   TransactionReference
            //   Amount             (decimal as "0.00", invariant culture)
            //   Status
            //   Optional1
            //   Optional2
            //   Optional3
            //   Optional4
            //   Optional5
            //   CurrencyCode
            //   IsTest             ("true" / "false")
            //   StatusMessage
            //   PrivateKey         (trailing salt — never sent on the wire)
            var sb = new StringBuilder(512);
            sb.Append(notification.SiteCode ?? string.Empty);
            sb.Append(notification.TransactionId ?? string.Empty);
            sb.Append(notification.TransactionReference ?? string.Empty);
            sb.Append(FormatAmount(notification.Amount));
            sb.Append(notification.Status ?? string.Empty);
            sb.Append(notification.Optional1 ?? string.Empty);
            sb.Append(notification.Optional2 ?? string.Empty);
            sb.Append(notification.Optional3 ?? string.Empty);
            sb.Append(notification.Optional4 ?? string.Empty);
            sb.Append(notification.Optional5 ?? string.Empty);
            sb.Append(notification.CurrencyCode ?? string.Empty);
            sb.Append(notification.IsTest ? "true" : "false");
            sb.Append(notification.StatusMessage ?? string.Empty);
            sb.Append(_settings.PrivateKey);

            var expected = Sha512HexLower(sb.ToString());

            // Constant-time compare avoids leaking the position of the first
            // mismatching byte to an attacker who can submit many forged
            // webhooks. Both strings are guaranteed hex-lowercase 128-char
            // outputs of SHA512.
            var expectedBytes = Encoding.ASCII.GetBytes(expected);
            var providedBytes = Encoding.ASCII.GetBytes(providedHash.Trim().ToLowerInvariant());

            if (expectedBytes.Length != providedBytes.Length)
            {
                _logger.LogWarning(
                    "Ozow notification hash length mismatch. Ref={Ref} Status={Status}",
                    notification.TransactionReference, notification.Status);
                return false;
            }

            var ok = CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
            if (!ok)
            {
                _logger.LogWarning(
                    "Ozow notification hash mismatch. Ref={Ref} Status={Status} IsTest={IsTest}",
                    notification.TransactionReference, notification.Status, notification.IsTest);
            }

            return ok;
        }

        /// <summary>
        /// Ozow expects amounts as decimal with two decimal places in the
        /// invariant ("en-US") form — e.g. <c>5.00</c>, not <c>5,00</c>.
        /// Both the wire body and the hash input MUST use this exact form,
        /// otherwise the hash they recompute server-side will not match.
        /// </summary>
        private static string FormatAmount(decimal amount)
            => amount.ToString("0.00", CultureInfo.InvariantCulture);

        /// <summary>
        /// SHA512(lowercased(utf8(input))) returned as hex lowercase (128 chars).
        /// </summary>
        private static string Sha512HexLower(string input)
        {
            var lowered = input.ToLowerInvariant();
            var bytes = Encoding.UTF8.GetBytes(lowered);
            var digest = SHA512.HashData(bytes);
            return Convert.ToHexString(digest).ToLowerInvariant();
        }
    }
}
