namespace ZansiHustle.Application.Payments.Providers
{
    /// <summary>
    /// Verifies inbound Yoco webhook signatures per the Standard Webhooks
    /// spec (https://www.standardwebhooks.com/) — the same signing scheme
    /// used by Yoco's webhook product.
    ///
    /// Algorithm:
    ///   signed_content = "{webhook-id}.{webhook-timestamp}.{raw_body}"
    ///   secret_bytes   = base64decode(WebhookSigningSecret without "whsec_" prefix)
    ///   computed       = HMAC-SHA256(secret_bytes, signed_content)
    ///   expected       = "v1," + base64(computed)
    /// The header may carry multiple space-separated signatures
    /// ("v1,abc v1,def") — accepting any one match.
    ///
    /// Implementations MUST use a constant-time comparison.
    /// </summary>
    public interface IYocoSignatureService
    {
        /// <summary>
        /// True only when the webhook signing secret is configured. Callers
        /// must check this before relying on <see cref="VerifyWebhookSignature"/>.
        /// </summary>
        bool IsImplemented { get; }

        /// <summary>
        /// Verifies a Standard Webhooks signature header against the raw
        /// request body. Returns false if the secret is missing, any header
        /// is missing, the timestamp is unparseable, or no signature matches.
        /// </summary>
        bool VerifyWebhookSignature(
            string rawBody,
            string? webhookId,
            string? webhookTimestamp,
            string? webhookSignature);
    }
}
