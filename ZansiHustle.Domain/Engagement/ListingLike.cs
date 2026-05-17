using System;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Listings;

namespace ZansiHustle.Domain.Engagement
{
    /// <summary>
    /// "Heart" / like by a buyer on a normal seller <see cref="Listing"/>
    /// (products and services owned by Merchants — NOT marketplace casual
    /// listings, which use <see cref="MarketplaceListingLike"/>).
    ///
    /// One row per (UserId, ListingId). Enforced by a unique index in
    /// EF configuration so the controller / service can stay idempotent
    /// without race-condition juggling — a duplicate insert raises a
    /// SqlException the service swallows as "already liked, no-op".
    ///
    /// Likes are written transactionally alongside an increment of
    /// <see cref="Listing.LikeCount"/> so the parent's denormalised
    /// count stays accurate without a per-read aggregate query.
    /// </summary>
    public class ListingLike
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid ListingId { get; set; }
        public Listing? Listing { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
