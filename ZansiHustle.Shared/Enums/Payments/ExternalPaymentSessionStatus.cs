namespace ZansiHustle.Shared.Enums.Payments
{
    /// <summary>
    /// Lifecycle of an <c>ExternalPaymentSession</c> — a payment ZansiHustle
    /// processes on behalf of an approved external shop (e.g. ZansiTech).
    /// Terminal states reported to the shop's callback are exactly
    /// <see cref="Paid"/>, <see cref="Failed"/>, <see cref="Cancelled"/>,
    /// <see cref="Expired"/> — never an ambiguous "maybe" state.
    /// </summary>
    public enum ExternalPaymentSessionStatus
    {
        /// <summary>Session row created; Ozow has not yet been called (or that call is in flight).</summary>
        Pending = 1,

        /// <summary>Ozow PostPaymentRequest succeeded — a redirect URL exists and the buyer has not yet completed the bank flow.</summary>
        RedirectCreated = 2,

        /// <summary>Ozow signalled Pending/PendingInvestigation — buyer submitted the EFT, awaiting bank confirmation.</summary>
        Processing = 3,

        /// <summary>Terminal — Ozow confirmed the transaction complete and amount/currency matched.</summary>
        Paid = 4,

        /// <summary>Terminal — Ozow declined/erred, or amount/currency mismatched a "Complete" signal.</summary>
        Failed = 5,

        /// <summary>Terminal — buyer or bank cancelled the transaction.</summary>
        Cancelled = 6,

        /// <summary>Terminal — session aged out with no resolution (opportunistic, no background job required).</summary>
        Expired = 7,
    }
}
