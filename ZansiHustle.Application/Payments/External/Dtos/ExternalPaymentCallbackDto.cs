using System;

namespace ZansiHustle.Application.Payments.External.Dtos
{
    /// <summary>
    /// FROZEN wire contract — the JSON body POSTed to the shop's CallbackUrl.
    /// Serialize this EXACTLY ONCE per delivery attempt; the resulting UTF-8
    /// bytes are both what gets HMAC-signed and what gets sent — never
    /// re-serialize between signing and sending (would change whitespace and
    /// break ZansiTech's raw-body signature verification).
    /// </summary>
    public sealed class ExternalPaymentCallbackDto
    {
        public string ShopCode { get; set; } = string.Empty;
        public string ProviderSessionId { get; set; } = string.Empty;
        public string ExternalOrderId { get; set; } = string.Empty;
        public string ExternalOrderNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;

        /// <summary>Always one of: Paid, Failed, Cancelled, Expired.</summary>
        public string Status { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }
}
