using System;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Public-safe projection of a merchant for unauthenticated discovery
    /// (Store Locator / Nearby). Returned by
    /// <c>GET /api/merchants/public</c> and
    /// <c>GET /api/merchants/public/{id}</c>.
    ///
    /// Deliberately a separate type from <see cref="MerchantDto"/> so the
    /// admin / owner DTO and its bank, KYC, payout, referral, contact-email,
    /// owner-id and revenue fields can never be reflected onto a public
    /// route by accident. <strong>Do not add private fields here.</strong>
    /// If you need a new public field, audit it for sensitivity first.
    /// </summary>
    public sealed class MerchantPublicDto
    {
        public Guid Id { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>
        /// Whether this merchant is a physical Store (people can visit
        /// the location) or an online-only Shop. Different surfaces
        /// gate on this:
        ///   • Nearby / Store Locator filters to <c>PhysicalStore</c>.
        ///   • Online product / service discovery treats both equally.
        /// Same int values as the admin <see cref="MerchantDto.Type"/>;
        /// safe to expose because it carries no PII.
        /// </summary>
        public MerchantType Type { get; set; }

        /// <summary>
        /// Convenience flag for clients that don't want to import the
        /// enum. <c>true</c> iff <c>Type == PhysicalStore</c>.
        /// </summary>
        public bool IsPhysicalStore { get; set; }

        /// <summary>
        /// Flat category name (joined from <c>SellerCategory.Name</c>).
        /// The frontend already prettifies whatever string we send, so we
        /// pass it through verbatim — no slug-vs-display-label split here.
        /// </summary>
        public string? Category { get; set; }

        // Location — every field is nullable because merchants onboarded
        // before structured-address capture may not have all of them.
        public string? Province { get; set; }
        public string? City { get; set; }
        public string? Suburb { get; set; }
        public string? AddressLine1 { get; set; }
        public string? FormattedAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        /// <summary>
        /// Computed when the caller supplies <c>lat</c> + <c>lng</c> query
        /// params; null otherwise. Two decimal places (~1.1 km) is plenty
        /// for the Nearby tab — exact distance is recomputed client-side
        /// once the user moves anyway.
        /// </summary>
        public decimal? DistanceKm { get; set; }

        // Trust signals
        /// <summary>
        /// True iff <c>Status == Active</c> AND <c>KycStatus == Verified</c>.
        /// The endpoint only returns Active merchants, so in practice this
        /// degenerates to "is KYC verified" — but the dual-check keeps the
        /// rule stable if the endpoint ever loosens its status filter.
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Null when <c>ReviewCount == 0</c>. Mirrors the rule already
        /// applied on the admin DTO: a stored rating is meaningless until
        /// at least one review has landed.
        /// </summary>
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }

        // Media
        public string? LogoUrl { get; set; }
        public string? BannerUrl { get; set; }

        // Public contact
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }
        public string? WebsiteUrl { get; set; }

        // ── Engagement (physical-store "save" only) ─────────────────
        /// <summary>
        /// Denormalised count of buyers who have saved this merchant.
        /// Only meaningful when <see cref="Type"/> is <c>PhysicalStore</c>;
        /// the field still serialises for other types (always 0) so
        /// the DTO shape stays uniform.
        /// </summary>
        public int SavesCount { get; set; }
        /// <summary>True when the authenticated caller has saved this store. False on anonymous reads.</summary>
        public bool IsSavedByMe { get; set; }
    }
}
