using System;
using System.Security.Cryptography;
using System.Text;
using ZansiHustle.Application.Payments.External;

namespace ZansiHustle.Infrastructure.Payments.External
{
    /// <summary>
    /// HMAC-SHA256 signer for the ZansiTech callback contract.
    ///   key     = UTF-8 bytes of the shop's shared secret
    ///   message = the exact raw callback body bytes (caller must pass the
    ///             SAME bytes it is about to send — never re-serialize)
    ///   output  = lowercase hex
    /// No timestamp/nonce — matches ZansiTech's current verifier exactly.
    /// Verification uses a constant-time comparison.
    /// </summary>
    public sealed class ExternalShopSignatureService : IExternalShopSignatureService
    {
        public string ComputeSignatureHex(string sharedSecret, byte[] rawBodyBytes)
        {
            if (string.IsNullOrEmpty(sharedSecret)) throw new ArgumentException("Shared secret is required.", nameof(sharedSecret));
            ArgumentNullException.ThrowIfNull(rawBodyBytes);

            var keyBytes = Encoding.UTF8.GetBytes(sharedSecret);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(rawBodyBytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public bool VerifySignatureHex(string sharedSecret, byte[] rawBodyBytes, string? providedHex)
        {
            if (string.IsNullOrWhiteSpace(providedHex)) return false;

            var expected = ComputeSignatureHex(sharedSecret, rawBodyBytes);
            var expectedBytes = Encoding.ASCII.GetBytes(expected);
            var providedBytes = Encoding.ASCII.GetBytes(providedHex.Trim().ToLowerInvariant());

            if (expectedBytes.Length != providedBytes.Length) return false;
            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }
    }
}
