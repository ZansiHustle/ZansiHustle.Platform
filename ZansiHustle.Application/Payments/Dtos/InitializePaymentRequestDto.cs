using System;

namespace ZansiHustle.Application.Payments.Dtos
{
    /// <summary>
    /// Buyer-initiated request to start a payment against an order they own.
    /// </summary>
    public class InitializePaymentRequestDto
    {
        public Guid OrderId { get; set; }

        /// <summary>
        /// Optional payment provider to route through. Accepted values:
        /// "Ozow" (live), "Paystack" (pending). When omitted or empty,
        /// the service defaults to Ozow — the only currently active provider.
        /// </summary>
        public string? Provider { get; set; }

        /// <summary>Optional — client may pass a preferred return/callback URL; falls back to the server default.</summary>
        public string? CallbackUrl { get; set; }
    }
}
