using System;

namespace ZansiHustle.Application.Seller.AccountProfile.Dtos
{
    /// <summary>
    /// Read model for the Seller Account Profile / control-centre screen.
    /// Aggregates the seller's <c>OnlineStore</c> Merchant identity, real
    /// counts, real order stats and visibility state. The trust SCORE is left
    /// null in this first pass (honest "not enough data yet") — a real
    /// ZansiPulse-backed score is wired in a later pass; never faked.
    /// </summary>
    public class SellerAccountProfileDto
    {
        public string SellerId { get; set; } = string.Empty;
        public Guid? MerchantId { get; set; }

        public string DisplayName { get; set; } = string.Empty;
        public string? TradingName { get; set; }
        public string? Category { get; set; }
        public string? LocationSummary { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        /// <summary>Public seller profile picture (the face shown to buyers).
        /// Separate from the private KYC selfie. Null when not yet uploaded.</summary>
        public string? PublicProfileImageUrl { get; set; }

        /// <summary>Approval lifecycle (e.g. "Active", "Pending", "Suspended").</summary>
        public string ApprovalStatus { get; set; } = string.Empty;

        /// <summary>"visible" | "paused" | "under_review" | "blocked".</summary>
        public string VisibilityStatus { get; set; } = "visible";
        public bool IsPaused { get; set; }
        public string? PauseReason { get; set; }
        public DateTime? PausedAtUtc { get; set; }
        public bool RequiresReview { get; set; }
        public string? ReviewReason { get; set; }
        public DateTime? ReviewSubmittedAtUtc { get; set; }

        /// <summary>Null until a real ZansiPulse score is available → UI shows
        /// "Not enough data yet". Never a fabricated number.</summary>
        public decimal? SellerScore { get; set; }
        public string? SellerScoreBand { get; set; }
        public decimal? SellerRating { get; set; }
        public int SellerReviewCount { get; set; }

        // Contact (read-only on the Contact screen for now).
        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public string? WhatsAppNumber { get; set; }

        // Pickup / collection address (read-only on the Pickup screen for now).
        public string? PickupAddressLine1 { get; set; }
        public string? PickupCity { get; set; }
        public string? PickupProvince { get; set; }
        public string? PickupPostalCode { get; set; }

        public int ProductsCount { get; set; }
        public int ServicesCount { get; set; }
        public int ShopCount { get; set; }
        public int ActiveListingsCount { get; set; }

        public SellerOrderStatsDto OrderStats { get; set; } = new();
        public SellerSetupStatusDto Setup { get; set; } = new();
        public SellerEditableSectionsDto EditableSections { get; set; } = new();
    }

    /// <summary>Real product-order behaviour for the caller's merchants.</summary>
    public class SellerOrderStatsDto
    {
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CancelledCount { get; set; }
        /// <summary>Completed ÷ accepted, 0–1. Null when no accepted orders yet.</summary>
        public decimal? CompletionRate { get; set; }
    }

    /// <summary>Which onboarding pieces are present (real flags, no fakes).</summary>
    public class SellerSetupStatusDto
    {
        public bool HasVerifiedIdentity { get; set; }
        public bool HasDocuments { get; set; }
        public bool HasPickupAddress { get; set; }
        public bool HasContactDetails { get; set; }
    }

    /// <summary>Which profile sections the seller may edit right now.</summary>
    public class SellerEditableSectionsDto
    {
        public bool TradingProfile { get; set; }
        public bool Contact { get; set; }
        public bool Verification { get; set; }
        public bool Visibility { get; set; }
    }

    /// <summary>Body for <c>PUT /api/seller/account-profile/visibility</c>.</summary>
    public class SellerVisibilityRequestDto
    {
        public bool IsPaused { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Body for <c>PUT /api/seller/account-profile/trading-profile</c> — the
    /// NON-sensitive, public-facing identity fields a seller may edit without
    /// triggering review. Sensitive changes (legal name / KYC docs / new profile
    /// photo when none is approved) are handled by the Verification flow.
    /// Null = leave unchanged.
    /// </summary>
    public class SellerTradingProfileRequestDto
    {
        public string? TradingName { get; set; }
        public string? Bio { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? ProfileImageUrl { get; set; }
    }

    /// <summary>
    /// Body for <c>PUT /api/seller/account-profile/pickup-address</c> — the
    /// courier-collection address for product orders. This is operational
    /// (private) data, distinct from the public area shown to buyers, though
    /// it currently shares the merchant's structured-address columns. Captured
    /// via Google Places on the client; geo fields help courier accuracy.
    /// Null leaves a field unchanged where sensible; AddressLine1 is required.
    /// </summary>
    public class SellerPickupAddressRequestDto
    {
        public string? AddressLine1 { get; set; }
        public string? Suburb { get; set; }
        public string? City { get; set; }
        public string? Province { get; set; }
        public string? PostalCode { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? GooglePlaceId { get; set; }
        public string? FormattedAddress { get; set; }
        public string? Country { get; set; }
        public string? CountryCode { get; set; }
    }
}
