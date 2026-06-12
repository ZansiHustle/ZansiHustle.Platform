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

        // ── Wallet-as-payment intent (server is the source of truth) ─────────
        /// <summary>True to apply wallet balance toward this order before charging
        /// the external gateway for the remainder.</summary>
        public bool UseWallet { get; set; }

        /// <summary>Preferred wallet amount to apply. The backend clamps it to
        /// min(requested, availableBalance, orderTotal). Null/0 with UseWallet=true
        /// means "use the maximum available". Frontend-calculated amounts are NEVER
        /// trusted — this is only a hint.</summary>
        public decimal? WalletAmountRequested { get; set; }
    }
}
