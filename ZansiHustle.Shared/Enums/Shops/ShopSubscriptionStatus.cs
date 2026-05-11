namespace ZansiHustle.Shared.Enums.Shops
{
    /// <summary>
    /// Billing / subscription state of a <c>ShopProfile</c>. Tracked
    /// from day one (with <see cref="EarlyAccess"/> as the default for
    /// new shops) so the eventual paid-plan rollout doesn't need a
    /// migration — only new values become reachable.
    ///
    ///   • <see cref="None"/>         — shop predates billing or was migrated in without a sub.
    ///   • <see cref="EarlyAccess"/>  — free during the early-access window. Default for new shops today.
    ///   • <see cref="Trial"/>        — paid plan, in trial period.
    ///   • <see cref="Active"/>       — paid plan, paid up to date.
    ///   • <see cref="PastDue"/>      — paid plan, payment failed but grace window not yet expired.
    ///   • <see cref="Cancelled"/>    — seller cancelled; access continues until SubscriptionEndsAtUtc.
    ///   • <see cref="Expired"/>      — cancelled OR PastDue past the grace window; shop should be Suspended.
    /// </summary>
    public enum ShopSubscriptionStatus
    {
        None = 1,
        EarlyAccess = 2,
        Trial = 3,
        Active = 4,
        PastDue = 5,
        Cancelled = 6,
        Expired = 7
    }
}
