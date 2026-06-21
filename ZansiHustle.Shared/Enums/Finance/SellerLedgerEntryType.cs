namespace ZansiHustle.Shared.Enums.Finance;

/// <summary>
/// What a seller ledger entry represents on the seller's append-only book.
/// </summary>
public enum SellerLedgerEntryType
{
    /// <summary>Seller net proceeds credited for an eligible paid order/booking.</summary>
    SellerNetCredit = 1,

    /// <summary>Reversal of a previously credited seller net (refund/cancellation).</summary>
    RefundReversal = 2,

    /// <summary>Debit recorded when proceeds are paid out to the seller.</summary>
    PayoutDebit = 3,

    /// <summary>Manual accounting adjustment.</summary>
    ManualAdjustment = 4
}
