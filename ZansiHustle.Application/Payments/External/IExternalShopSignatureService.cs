namespace ZansiHustle.Application.Payments.External
{
    /// <summary>
    /// HMAC-SHA256 signing for the ZansiTech callback contract. Frozen spec:
    /// key = UTF-8 bytes of the shop's shared secret; message = the EXACT raw
    /// callback body bytes; output = lowercase hex. No timestamp/nonce —
    /// ZansiTech's current verifier does not expect one; do not add one here
    /// without updating both sides.
    /// </summary>
    public interface IExternalShopSignatureService
    {
        string ComputeSignatureHex(string sharedSecret, byte[] rawBodyBytes);

        bool VerifySignatureHex(string sharedSecret, byte[] rawBodyBytes, string? providedHex);
    }
}
