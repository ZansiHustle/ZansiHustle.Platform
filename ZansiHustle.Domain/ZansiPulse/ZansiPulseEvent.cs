using System;
using ZansiHustle.Shared.Enums.ZansiPulse;

namespace ZansiHustle.Domain.ZansiPulse
{
    /// <summary>
    /// A single raw user-interaction signal captured by ZansiPulse — the
    /// append-only event stream that everything else (interest scoring,
    /// metrics, trending, recommendations, snapshots) is derived from.
    ///
    /// Reference columns (<see cref="ListingId"/>, <see cref="SellerId"/>,
    /// etc.) are stored as loose, indexed <see cref="Guid"/> values WITHOUT
    /// foreign keys on purpose: this is an analytics log that must survive
    /// the deletion of the entity it points at, and hard FKs to Listings /
    /// Users / Merchants would create cascade-path conflicts and block
    /// operational deletes. Joins are performed at query time in the
    /// service layer. All timestamps are UTC.
    /// </summary>
    public class ZansiPulseEvent
    {
        public Guid Id { get; set; }

        /// <summary>The acting user, when the event came from an authenticated session. Null for anonymous/system signals.</summary>
        public Guid? UserId { get; set; }

        public ZansiPulseEventType EventType { get; set; }

        public Guid? ListingId { get; set; }

        /// <summary>The seller this event relates to — a <c>Merchant</c> id.</summary>
        public Guid? SellerId { get; set; }

        /// <summary>The shop storefront this event relates to — a <c>ShopProfile</c> id.</summary>
        public Guid? ShopId { get; set; }

        /// <summary>Seller-category id (<c>SellerCategory</c>).</summary>
        public Guid? CategoryId { get; set; }

        /// <summary>Seller-subcategory id (<c>SellerSubcategory</c>).</summary>
        public Guid? SubCategoryId { get; set; }

        public string? SearchTerm { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }

        /// <summary>Listing/transaction price at the time of the event, when relevant.</summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Behavioural weight applied for this event (resolved from settings
        /// at capture time). Positive for engagement, negative for
        /// hide/not-interested/report. Persisted so later re-scoring is
        /// reproducible even if the settings change.
        /// </summary>
        public decimal Weight { get; set; }

        /// <summary>Optional free-form JSON payload for context not modelled as columns.</summary>
        public string? MetadataJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
