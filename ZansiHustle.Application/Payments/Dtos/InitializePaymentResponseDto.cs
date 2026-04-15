using System;
using ZansiHustle.Shared.Enums.Payments;

namespace ZansiHustle.Application.Payments.Dtos
{
    /// <summary>
    /// Returned by <c>POST /api/payments/initialize</c>. Client redirects the
    /// user to <see cref="AuthorizationUrl"/> and then either waits for the webhook
    /// or polls <c>GET /api/payments/{id}</c> / <c>POST /api/payments/verify/{reference}</c>.
    /// </summary>
    public class InitializePaymentResponseDto
    {
        public Guid PaymentId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string? ProviderReference { get; set; }
        public string? AuthorizationUrl { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public PaymentTransactionStatus Status { get; set; }

        /// <summary>
        /// Paystack Public Key echoed back so in-app native checkout (via a
        /// Paystack SDK) can run without needing a second config request.
        /// Safe to expose.
        /// </summary>
        public string? PublicKey { get; set; }
    }
}
