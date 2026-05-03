using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Infrastructure.Configuration;

namespace ZansiHustle.Infrastructure.Payments.Ozow
{
    /// <summary>
    /// SHA512 hash service for Ozow.
    ///
    /// ─── INTENTIONALLY UNIMPLEMENTED ────────────────────────────────────────
    /// The Ozow hash specification (the exact ordered list of fields, the
    /// case-folding rule, the boolean serialization for IsTest, and the
    /// trailing PrivateKey position) is NOT yet checked into this repository.
    ///
    /// Per the team rule: do not guess hash logic. Adding a wrong field
    /// ordering would either:
    ///   • silently fail against Ozow (every PostPaymentRequest rejected with
    ///     "Hash mismatch" — a deploy-time outage), or
    ///   • silently accept forged webhooks (a security incident — anyone who
    ///     can POST to NotifyUrl could mark orders as paid).
    ///
    /// Until the canonical Ozow hash spec lives in
    ///   docs/payments/ozow-hash-spec.md
    /// (with explicit field order for both the request and the notification),
    /// this service returns IsImplemented = false and refuses to compute or
    /// validate hashes. The rest of the integration (settings, DTOs, client,
    /// service routing, webhook controller, idempotent state machine) is in
    /// place and will become functional the moment the hash math is added.
    ///
    /// To complete:
    ///   1. Add docs/payments/ozow-hash-spec.md from Ozow's developer portal
    ///      ("How to generate the hashCheck" and "Validating the response
    ///      hash" sections).
    ///   2. Replace the two NotImplementedException paths below with the
    ///      ordered concatenation → ToLower → UTF8 → SHA512 → hex pipeline,
    ///      appending OzowSettings.PrivateKey as the trailing salt.
    ///   3. Flip <see cref="IsImplemented"/> to return true when both the
    ///      spec is wired AND PrivateKey is configured.
    ///   4. Use CryptographicOperations.FixedTimeEquals for the validate path.
    /// ─────────────────────────────────────────────────────────────────────────
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

        // TODO(ozow-hash): flip to `!string.IsNullOrWhiteSpace(_settings.PrivateKey)`
        // once the canonical hash spec is added to the repo and the methods
        // below are filled in. Keeping this hard false ensures no fake hash
        // is ever produced or trusted.
        public bool IsImplemented => false;

        public string GenerateRequestHash(OzowPaymentRequest request)
        {
            // TODO(ozow-hash): implement per docs/payments/ozow-hash-spec.md.
            // Required pipeline (per Ozow docs once added to repo):
            //   1. Concatenate the canonical Ozow request fields in the
            //      documented order. The trailing salt is _settings.PrivateKey.
            //   2. ToLowerInvariant the concatenated string.
            //   3. UTF8 → SHA512 → hex (lowercase).
            // Field order MUST be copied verbatim from Ozow's docs; do not
            // infer it from this DTO's property order.
            _logger.LogError("OzowHashService.GenerateRequestHash called before the hash spec was implemented. See OzowHashService.cs TODO.");
            throw new NotImplementedException(
                "Ozow request-hash generation is not yet implemented. " +
                "Add docs/payments/ozow-hash-spec.md and complete OzowHashService.GenerateRequestHash before calling this. " +
                "Callers must guard with IOzowHashService.IsImplemented.");
        }

        public bool ValidateNotificationHash(OzowTransactionNotification notification)
        {
            // Until the spec is in the repo, never accept any hash as valid.
            // This intentionally blocks marking payments as paid via a forged
            // webhook in the misconfigured state.
            if (notification is null) return false;

            _logger.LogWarning(
                "OzowHashService.ValidateNotificationHash called before the hash spec was implemented. " +
                "Returning false (safe). Notification for TransactionReference={Ref} will be parked for ops review.",
                notification.TransactionReference);

            // TODO(ozow-hash): once spec is available, implement:
            //   1. Concatenate the canonical Ozow notification fields in the
            //      documented order, with _settings.PrivateKey as trailing salt.
            //   2. ToLowerInvariant.
            //   3. UTF8 → SHA512 → hex (lowercase).
            //   4. Constant-time compare against notification.Hash via
            //      CryptographicOperations.FixedTimeEquals.
            return false;
        }
    }
}
