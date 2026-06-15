namespace ZansiHustle.Shared.Enums.Merchants;

/// <summary>
/// Category of a seller fulfilment incident. Foundation for accountability when
/// a seller accepts an order but fails to deliver on it. Integer-backed.
/// </summary>
public enum SellerIncidentType
{
    /// <summary>Seller accepted but had no stock.</summary>
    AcceptedButNoStock = 1,
    /// <summary>Seller did not show / was unavailable for courier pickup.</summary>
    SellerNoShowPickup = 2,
    /// <summary>Seller cancelled after having accepted the order.</summary>
    SellerCancelledAfterAcceptance = 3,
    /// <summary>Courier pickup failed (seller-side).</summary>
    FailedPickup = 4,
    /// <summary>Repeated fulfilment delays.</summary>
    RepeatedDelay = 5,
    /// <summary>Manually recorded by an admin.</summary>
    ManualAdminPenalty = 100,
}

/// <summary>Severity of a seller incident (informational; drives default amounts later).</summary>
public enum SellerIncidentSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
}

/// <summary>
/// Lifecycle of a seller incident. Defaults to <see cref="PendingReview"/> — V1
/// records incidents but does NOT auto-charge; an admin explicitly applies.
/// </summary>
public enum SellerIncidentStatus
{
    /// <summary>Recorded, awaiting an admin decision. No payout impact yet.</summary>
    PendingReview = 1,
    /// <summary>Applied — reduces the seller's payout/earnings.</summary>
    Applied = 2,
    /// <summary>Reviewed and dismissed — no impact.</summary>
    Waived = 3,
    /// <summary>Previously applied, then reversed.</summary>
    Reversed = 4,
}
