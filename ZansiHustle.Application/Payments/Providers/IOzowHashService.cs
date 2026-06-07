using System.Collections.Generic;

namespace ZansiHustle.Application.Payments.Providers
{
    /// <summary>
    /// Single field's contribution to the hash, captured for diagnostic
    /// purposes. <c>HashValue</c> is the exact string the hash service
    /// concatenated; <c>BodyValue</c> is the exact string the request DTO
    /// will serialise. When the two diverge we have a hash/body mismatch
    /// that Ozow will reject with "HashCheck value has failed".
    /// PrivateKey is intentionally absent from this record.
    /// </summary>
    public sealed record OzowHashField(string Name, string BodyValue, string HashValue);

    /// <summary>
    /// Diagnostic snapshot returned alongside the computed hash. Renders
    /// every hashable field, plus boot-time integrity flags
    /// (<c>PrivateKeyTrimChanged</c>, <c>SiteCodeTrimChanged</c>, …) so
    /// silent whitespace bugs in env-var values surface in stdout. The
    /// hash itself is NOT included in this record — that's still secret.
    /// </summary>
    public sealed class OzowHashDiagnostic
    {
        public required IReadOnlyList<OzowHashField> Fields { get; init; }
        public required int PrivateKeyLength { get; init; }
        public required bool PrivateKeyTrimChanged { get; init; }
        public required bool SiteCodeTrimChanged { get; init; }
        public required bool AnyUrlTrimChanged { get; init; }
        /// <summary>True when at least one field's hash-input string ≠ its body-render string. Triggers the precheck warning.</summary>
        public required bool AnyMismatch { get; init; }
    }

    /// <summary>
    /// SHA512 hash generation + validation for the Ozow integration.
    ///
    /// Two distinct hashes exist in the Ozow protocol:
    ///   1. <see cref="GenerateRequestHash"/> — outgoing PostPaymentRequest hashCheck
    ///   2. <see cref="ValidateNotificationHash"/> — incoming TransactionNotificationResponse hash
    ///
    /// Both follow Ozow's strict field-ordering convention with the merchant
    /// PrivateKey appended as the trailing salt. The exact field order MUST
    /// match Ozow's documentation; an incorrect order silently fails.
    ///
    /// IMPLEMENTATION STATUS:
    /// The exact field-ordering specification is not yet checked into this
    /// repository. <see cref="OzowHashService"/> currently refuses to compute
    /// or validate hashes until that spec is added — see the TODO inside the
    /// implementation. This is deliberate: a fake hash would either fail
    /// silently against Ozow (initiate calls rejected with bad-hash) or, worse,
    /// silently accept attacker-forged webhooks. <see cref="IsImplemented"/>
    /// returns false until the spec is wired in.
    /// </summary>
    public interface IOzowHashService
    {
        /// <summary>
        /// True only when the hash specification is implemented AND the
        /// merchant private key is configured. Callers must check this before
        /// calling <see cref="GenerateRequestHash"/> /
        /// <see cref="ValidateNotificationHash"/>.
        /// </summary>
        bool IsImplemented { get; }

        /// <summary>
        /// Computes the lowercase SHA512 hex hash for an outgoing
        /// <see cref="OzowPaymentRequest"/>. Throws if the spec is not yet
        /// implemented — callers must check <see cref="IsImplemented"/> first.
        /// </summary>
        string GenerateRequestHash(OzowPaymentRequest request);

        /// <summary>
        /// Same as <see cref="GenerateRequestHash"/> but also returns a
        /// per-field diagnostic snapshot so the caller can log a sanitized
        /// body-vs-hash comparison alongside the call. Used when a
        /// "HashCheck value has failed" response forces us to bisect which
        /// field is differing. PrivateKey is never returned.
        /// </summary>
        (string Hash, OzowHashDiagnostic Diagnostic) GenerateRequestHashWithDiagnostic(OzowPaymentRequest request);

        /// <summary>
        /// Validates the <c>Hash</c> field on an inbound
        /// <see cref="OzowTransactionNotification"/>. Returns false if the
        /// hash is missing, the spec is not yet implemented, or computed
        /// hash does not match. Uses constant-time comparison.
        /// </summary>
        bool ValidateNotificationHash(OzowTransactionNotification notification);
    }
}
