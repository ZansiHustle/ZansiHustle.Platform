namespace ZansiHustle.Shared.Enums.ZansiDispatch;

// ─────────────────────────────────────────────────────────────────────────────
// ZansiDispatch — the ZansiHustle logistics control layer. All enums for the
// module live here. Values are explicit so reordering never silently remaps
// stored data. Stored as int.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Which logistics provider produced a quote / owns a shipment.</summary>
public enum ZansiDispatchProviderType
{
    /// <summary>Deterministic in-house estimate from ZansiDispatch settings. Phase 1 default.</summary>
    InternalEstimate = 1,
    /// <summary>Flat safety-net estimate when no other provider can quote.</summary>
    ManualFallback = 2,
    /// <summary>The Courier Guy (Shiplogic API). Reserved — not wired in Phase 1.</summary>
    CourierGuy = 3,
    /// <summary>Shiplogic direct. Reserved — not wired in Phase 1.</summary>
    Shiplogic = 4,
    /// <summary>Placeholder for any future provider.</summary>
    FutureProvider = 5,
}

/// <summary>Lifecycle of a checkout delivery quote.</summary>
public enum ZansiDispatchQuoteStatus
{
    Draft = 1,
    Presented = 2,
    Selected = 3,
    Expired = 4,
    ConvertedToOrder = 5,
    Cancelled = 6,
}

/// <summary>
/// Delivery lifecycle of a shipment after order creation. Values 1-9 are
/// stable (data may exist); new states are appended with new ints.
/// </summary>
public enum ZansiDispatchShipmentStatus
{
    PendingDispatch = 1,
    PreparingPickup = 2,
    BookedWithCourier = 3,
    PickedUp = 4,
    InTransit = 5,
    Delivered = 6,
    Failed = 7,
    Cancelled = 8,
    Returned = 9,
    OutForDelivery = 10,
    Exception = 11,
    OnHold = 12,
}

/// <summary>Reconciliation state of a shipment's delivery-fee vs actual courier cost.</summary>
public enum ZansiDispatchReconciliationStatus
{
    Pending = 1,
    ActualCostCaptured = 2,
    Reconciled = 3,
    Disputed = 4,
    Adjusted = 5,
}

/// <summary>Delivery speed / fulfilment type of a quote option.</summary>
public enum ZansiDispatchServiceLevel
{
    Standard = 1,
    Express = 2,
    Economy = 3,
    /// <summary>Buyer + seller arrange collection; no delivery fee.</summary>
    Collection = 4,
}

/// <summary>Type of a delivery-fee ledger movement.</summary>
public enum ZansiDispatchLedgerEntryType
{
    QuoteCharged = 1,
    ActualCourierCost = 2,
    SurplusRecognised = 3,
    DeficitRecognised = 4,
    ManualAdjustment = 5,
    Refund = 6,
    DiscountFunded = 7,
}

/// <summary>Ledger direction for a movement.</summary>
public enum ZansiDispatchLedgerDirection
{
    Credit = 1,
    Debit = 2,
}

/// <summary>Coarse parcel size band used by the internal estimate.</summary>
public enum ZansiDispatchItemSizeCategory
{
    Small = 1,
    Medium = 2,
    Large = 3,
    ExtraLarge = 4,
}

/// <summary>Provider operation captured in the request/response log.</summary>
public enum ZansiDispatchProviderOperation
{
    GetRates = 1,
    CreateShipment = 2,
    GetStatus = 3,
    CancelShipment = 4,
    GetLabel = 5,
    Webhook = 6,
}

/// <summary>
/// Courier address type (mirrors Courier Guy / Shiplogic <c>type</c>). Stored
/// as int. Defaults to <see cref="Residential"/> for buyer drop-offs and
/// <see cref="Business"/> for seller collections when unspecified.
/// </summary>
public enum ZansiDispatchAddressType
{
    Residential = 1,
    Business = 2,
    Counter = 3,
    Locker = 4,
    Unknown = 5,
}
