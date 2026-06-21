namespace ZansiHustle.Shared.Enums.Finance;

/// <summary>
/// What a platform ledger entry represents on the platform's append-only book.
/// Gateway fee is a COST (not profit); delivery is a pass-through.
/// </summary>
public enum PlatformLedgerEntryType
{
    /// <summary>ZansiHustle platform fee — platform revenue (5%).</summary>
    PlatformFeeRevenue = 1,

    /// <summary>Gateway (Ozow) fee — a cost, NOT platform profit (3%).</summary>
    GatewayFeeCost = 2,

    /// <summary>Delivery/courier fee recorded as a pass-through (not revenue).</summary>
    DeliveryFeePassThrough = 3,

    /// <summary>Reversal of a previously recorded platform entry.</summary>
    RefundReversal = 4,

    /// <summary>Manual accounting adjustment.</summary>
    Adjustment = 5
}
