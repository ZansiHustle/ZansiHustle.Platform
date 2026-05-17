using System;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Shops;

namespace ZansiHustle.Domain.Engagement
{
    /// <summary>
    /// "Follow" relationship between a buyer and a
    /// <see cref="ShopProfile"/>. Distinct from a per-listing like —
    /// following a shop expresses interest in the brand and its future
    /// listings, and will later be used to bias the Home feed.
    ///
    /// One row per (UserId, ShopProfileId). Follower count is
    /// denormalised onto <see cref="ShopProfile.FollowersCount"/> and
    /// updated transactionally with this row.
    ///
    /// Note: there is also a legacy <c>Merchant.FollowersCount</c>
    /// column. It pre-dates the ShopProfile split and was never wired
    /// to a real follow source. We deliberately introduce a new
    /// <c>ShopProfile.FollowersCount</c> here so follows attach to the
    /// storefront (which is what buyers see) rather than to the
    /// payout-merchant record.
    /// </summary>
    public class ShopFollow
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid ShopProfileId { get; set; }
        public ShopProfile? ShopProfile { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
