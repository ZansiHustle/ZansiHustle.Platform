using System;
using System.Collections.Generic;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.SellerCategories;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Shared.Enums.Listings;

namespace ZansiHustle.Domain.Listings
{
    /// <summary>
    /// Unified product / service listing owned by a <see cref="Merchant"/>.
    /// Type-specific fields (e.g. Stock, PricingModel) are nullable and only
    /// populated for the corresponding <see cref="ListingType"/>.
    /// </summary>
    public class Listing
    {
        public Guid Id { get; set; }

        /// <summary>Short opaque business code (e.g. <c>LIS-20260415...</c>).</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>URL-safe unique slug derived from the title at creation.</summary>
        public string Slug { get; set; } = string.Empty;

        public ListingType Type { get; set; }
        public ListingStatus Status { get; set; } = ListingStatus.Active;

        /// <summary>
        /// Where this listing is available — gates which surfaces may
        /// render it (Home / Explore feeds vs. Store profile only) and
        /// whether online checkout applies. Defaults to
        /// <see cref="AvailabilityMode.OnlineOnly"/> at the database
        /// level (migration backfill); <c>ListingService</c> overrides
        /// the default at create-time based on the owning merchant's
        /// type (<c>PhysicalStore → InStoreOnly</c>).
        /// </summary>
        public AvailabilityMode AvailabilityMode { get; set; } = AvailabilityMode.OnlineOnly;

        /// <summary>Owning merchant. Required — a listing always belongs to a merchant.</summary>
        public Guid MerchantId { get; set; }
        public Merchant? Merchant { get; set; }

        /// <summary>
        /// The sales channel this listing was created under. Decides
        /// which buyer surfaces render it and what display-owner pill
        /// the card shows. See <see cref="ListingSource"/> for the
        /// per-value contract. Defaults to SellerAccount so legacy
        /// rows (created before this column existed) still appear in
        /// the seller's My Listings without dragging into a shop they
        /// were never listed under.
        /// </summary>
        public ListingSource ListingSource { get; set; } = ListingSource.SellerAccount;

        /// <summary>
        /// FK to <see cref="ShopProfile"/> when (and only when)
        /// <see cref="ListingSource"/> is <c>ShopProfile</c>. The
        /// ShopProfile's <c>MerchantId</c> MUST equal this listing's
        /// <c>MerchantId</c> — same merchant owns both. Buyer-facing
        /// shop catalog filters by THIS column (not by MerchantId) so
        /// SellerAccount listings stay out of the shop's catalog.
        /// </summary>
        public Guid? ShopProfileId { get; set; }
        public ShopProfile? ShopProfile { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Unit price. For services with PricingModel.Quote/Negotiable this may be 0.</summary>
        public decimal Price { get; set; }
        public string Currency { get; set; } = "ZAR";

        public Guid? SellerCategoryId { get; set; }
        public SellerCategory? SellerCategory { get; set; }

        public Guid? SellerSubcategoryId { get; set; }
        public SellerSubcategory? SellerSubcategory { get; set; }

        public string? Province { get; set; }
        public string? City { get; set; }

        /// <summary>Ordered list of image URLs (v1 stores URLs only — no upload).</summary>
        public List<string> Images { get; set; } = new();

        public bool IsFeatured { get; set; }
        public bool IsBoosted { get; set; }

        // Aggregate review metrics (no per-review entity yet).
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }

        // Product-only fields.
        public int? Stock { get; set; }
        public ListingCondition? Condition { get; set; }
        public List<string>? DeliveryOptions { get; set; }

        // Service-only fields.
        public PricingModel? PricingModel { get; set; }
        public string? ServiceArea { get; set; }
        public string? Turnaround { get; set; }
        public List<string>? Availability { get; set; }
        public List<string>? BookingMethods { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
