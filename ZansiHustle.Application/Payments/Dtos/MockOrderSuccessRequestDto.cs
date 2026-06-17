using System;

namespace ZansiHustle.Application.Payments.Dtos
{
    /// <summary>
    /// Dev/UAT-only request to simulate a successful gateway payment for an
    /// order the caller owns — used to exercise the product-order →
    /// seller-acceptance → dispatch flow without paying real money through Ozow.
    /// Only honoured when <c>Payments:MockCheckoutEnabled</c> is true (and never
    /// in Production). Settles the order through the SAME internal paid-transition
    /// path as a real Ozow success, so PaymentStatus, order status, notifications,
    /// and wallet handling all behave identically.
    /// </summary>
    public sealed class MockOrderSuccessRequestDto
    {
        public Guid OrderId { get; set; }

        /// <summary>Provider label to record on the mock payment (defaults to "Ozow").</summary>
        public string? Provider { get; set; }

        /// <summary>Optional caller-supplied reference. When omitted the service
        /// generates a clearly-marked <c>MOCK-OZOW-{orderCode}-{timestamp}</c>.</summary>
        public string? MockReference { get; set; }

        // ── Wallet intent (mirrors InitializePaymentRequestDto) ──────────────
        /// <summary>Apply wallet balance toward the order before the mock external
        /// leg. The backend clamps + holds exactly as the real flow does, so a
        /// split is never double-charged.</summary>
        public bool UseWallet { get; set; }

        /// <summary>Preferred wallet amount (a HINT — backend clamps to
        /// min(requested, balance, total)). Null with UseWallet=true means max.</summary>
        public decimal? WalletAmountRequested { get; set; }
    }
}
