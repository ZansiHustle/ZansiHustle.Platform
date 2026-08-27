using System.Security.Cryptography;
using System.Text;
using Xunit;
using ZansiHustle.Infrastructure.Payments.External;

namespace ZansiHustle.Tests.Payments.External
{
    /// <summary>
    /// Interoperability proof for the frozen ZansiTech callback signature
    /// contract: HMAC-SHA256(key = UTF-8 shared secret, message = raw body
    /// bytes) -> lowercase hex. No timestamp/nonce.
    /// </summary>
    public class ExternalShopSignatureServiceTests
    {
        [Fact]
        public void ComputeSignatureHex_MatchesIndependentHmacSha256Hex()
        {
            var service = new ExternalShopSignatureService();
            const string secret = "some-shared-secret";
            var body = Encoding.UTF8.GetBytes("{\"shopCode\":\"zansitech\",\"status\":\"Paid\"}");

            var actual = service.ComputeSignatureHex(secret, body);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expected = System.Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();

            Assert.Equal(expected, actual);
            Assert.Equal(64, actual.Length); // SHA-256 = 32 bytes = 64 hex chars
            Assert.Equal(actual, actual.ToLowerInvariant()); // lowercase per contract
        }

        [Fact]
        public void ComputeSignatureHex_DifferentBody_ProducesDifferentSignature()
        {
            var service = new ExternalShopSignatureService();
            const string secret = "some-shared-secret";

            var a = service.ComputeSignatureHex(secret, Encoding.UTF8.GetBytes("body-a"));
            var b = service.ComputeSignatureHex(secret, Encoding.UTF8.GetBytes("body-b"));

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void VerifySignatureHex_AcceptsValidSignature_RejectsTamperedBody()
        {
            var service = new ExternalShopSignatureService();
            const string secret = "some-shared-secret";
            var body = Encoding.UTF8.GetBytes("{\"amount\":100}");

            var signature = service.ComputeSignatureHex(secret, body);

            Assert.True(service.VerifySignatureHex(secret, body, signature));
            Assert.False(service.VerifySignatureHex(secret, Encoding.UTF8.GetBytes("{\"amount\":999}"), signature));
            Assert.False(service.VerifySignatureHex("wrong-secret", body, signature));
        }
    }
}
