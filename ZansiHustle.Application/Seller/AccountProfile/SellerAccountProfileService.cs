using System;
using System.Linq;
using System.Threading.Tasks;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Application.Persistence.Shops;
using ZansiHustle.Application.Seller.AccountProfile.Dtos;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Seller.AccountProfile
{
    /// <summary>
    /// Aggregates the seller's <c>OnlineStore</c> Merchant identity + real
    /// counts / order stats / visibility into one read model, and applies the
    /// seller-account visibility pause. The seller ACCOUNT is the OnlineStore
    /// merchant (products / services live under it; shops attach to it). A
    /// PhysicalStore merchant is a separate concern and not surfaced here.
    /// </summary>
    public sealed class SellerAccountProfileService : ISellerAccountProfileService
    {
        private readonly IMerchantRepository _merchants;
        private readonly IListingRepository _listings;
        private readonly IShopProfileRepository _shops;
        private readonly IOrderRepository _orders;

        public SellerAccountProfileService(
            IMerchantRepository merchants,
            IListingRepository listings,
            IShopProfileRepository shops,
            IOrderRepository orders)
        {
            _merchants = merchants;
            _listings = listings;
            _shops = shops;
            _orders = orders;
        }

        public async Task<Result<SellerAccountProfileDto>> GetMyProfileAsync(Guid ownerUserId)
        {
            try
            {
                if (ownerUserId == Guid.Empty)
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

                var merchant = await ResolveSellerMerchantAsync(ownerUserId);
                if (merchant is null)
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.NotFound, "No seller account found.");

                var dto = await BuildAsync(ownerUserId, merchant);
                return Result<SellerAccountProfileDto>.Success(dto, "Seller account profile loaded.");
            }
            catch (Exception ex)
            {
                return Result<SellerAccountProfileDto>.Failure(
                    ErrorCodes.Exception, $"An error occurred while loading your seller account. {ex.Message}");
            }
        }

        public async Task<Result<SellerAccountProfileDto>> SetVisibilityAsync(Guid ownerUserId, bool isPaused, string? reason)
        {
            try
            {
                if (ownerUserId == Guid.Empty)
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");

                var merchant = await ResolveSellerMerchantAsync(ownerUserId);
                if (merchant is null)
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.NotFound, "No seller account found.");

                // Must be approved to change public visibility.
                if (merchant.Status != MerchantStatus.Active)
                    return Result<SellerAccountProfileDto>.Failure(
                        ErrorCodes.Forbidden, "Your seller account isn't approved yet, so its visibility can't be changed.");

                // Admin-held states are not seller-serviceable.
                if (merchant.SellerVisibility == SellerVisibilityStatus.Blocked ||
                    merchant.SellerVisibility == SellerVisibilityStatus.UnderReview)
                {
                    return Result<SellerAccountProfileDto>.Failure(
                        ErrorCodes.Forbidden,
                        "Your seller account's visibility is managed by ZansiHustle and can't be changed here.");
                }

                var now = DateTime.UtcNow;
                if (isPaused)
                {
                    merchant.SellerVisibility = SellerVisibilityStatus.Paused;
                    merchant.SellerPausedAtUtc = now;
                    merchant.SellerPauseReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
                }
                else
                {
                    merchant.SellerVisibility = SellerVisibilityStatus.Visible;
                    merchant.SellerPausedAtUtc = null;
                    merchant.SellerPauseReason = null;
                }
                merchant.SellerVisibilityUpdatedAtUtc = now;
                merchant.UpdatedAtUtc = now;

                _merchants.Update(merchant);
                if (!await _merchants.SaveChangesAsync())
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.Exception, "Failed to update visibility.");

                var dto = await BuildAsync(ownerUserId, merchant);
                return Result<SellerAccountProfileDto>.Success(
                    dto, isPaused ? "Seller account paused." : "Seller account is live again.");
            }
            catch (Exception ex)
            {
                return Result<SellerAccountProfileDto>.Failure(
                    ErrorCodes.Exception, $"An error occurred while updating visibility. {ex.Message}");
            }
        }

        public async Task<Result<SellerAccountProfileDto>> UpdateTradingProfileAsync(Guid ownerUserId, SellerTradingProfileRequestDto request)
        {
            try
            {
                if (ownerUserId == Guid.Empty)
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.Unauthorized, "User identifier not found.");
                if (request is null)
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var merchant = await ResolveSellerMerchantAsync(ownerUserId);
                if (merchant is null)
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.NotFound, "No seller account found.");

                // SAFE public fields only — these never trigger review. Sensitive
                // changes (legal name / KYC docs / first profile photo) are NOT
                // handled here; they route through the Verification flow.
                // `TradingName` maps to Merchant.Name (the public display name);
                // the immutable Slug is left untouched so external links stay stable.
                if (!string.IsNullOrWhiteSpace(request.TradingName))
                    merchant.Name = request.TradingName.Trim();
                if (request.Bio != null) merchant.Description = Trim(request.Bio);
                if (request.City != null) merchant.City = Trim(request.City);
                if (request.Province != null) merchant.Province = Trim(request.Province);
                if (request.ProfileImageUrl != null) merchant.ProfileImageUrl = Trim(request.ProfileImageUrl);
                merchant.UpdatedAtUtc = DateTime.UtcNow;

                _merchants.Update(merchant);
                if (!await _merchants.SaveChangesAsync())
                    return Result<SellerAccountProfileDto>.Failure(ErrorCodes.Exception, "Failed to update trading profile.");

                var dto = await BuildAsync(ownerUserId, merchant);
                return Result<SellerAccountProfileDto>.Success(dto, "Trading profile updated.");
            }
            catch (Exception ex)
            {
                return Result<SellerAccountProfileDto>.Failure(
                    ErrorCodes.Exception, $"An error occurred while updating your trading profile. {ex.Message}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static string? Trim(string? value)
        {
            if (value is null) return null;
            var trimmed = value.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        private async Task<Merchant?> ResolveSellerMerchantAsync(Guid ownerUserId)
        {
            var merchants = await _merchants.GetByOwnerAsync(ownerUserId);
            // The seller ACCOUNT = the OnlineStore merchant. PhysicalStore is a
            // separate (Nearby) concern handled elsewhere.
            return merchants.FirstOrDefault(m => m.Type == MerchantType.OnlineStore);
        }

        private async Task<SellerAccountProfileDto> BuildAsync(Guid ownerUserId, Merchant merchant)
        {
            // Counts — shop-scope-agnostic; every listing under the merchant.
            var listings = await _listings.GetByMerchantAsync(merchant.Id);
            var active = listings.Where(l => l.Status == ListingStatus.Active).ToList();
            var productsCount = active.Count(l => l.Type == ListingType.Product);
            var servicesCount = active.Count(l => l.Type == ListingType.Service);

            var shop = await _shops.GetActiveByMerchantAsync(merchant.Id);
            var shopCount = shop is null ? 0 : 1;

            // Real product-order behaviour (caller's own merchants only).
            var orders = await _orders.GetBySellerUserAsync(ownerUserId);
            var acceptedCount = orders.Count(o =>
                o.Status == OrderStatus.Confirmed ||
                o.Status == OrderStatus.InProgress ||
                o.Status == OrderStatus.Completed);
            var completedCount = orders.Count(o => o.Status == OrderStatus.Completed);
            var cancelledCount = orders.Count(o => o.Status == OrderStatus.Cancelled);
            // Order-level rejection collapses into Cancelled (no distinct status),
            // so a separate "rejected" count isn't tracked here — kept 0 rather
            // than guessing. (Service-booking rejections are a later wire-up.)
            const int rejectedCount = 0;
            decimal? completionRate = acceptedCount > 0
                ? Math.Round((decimal)completedCount / acceptedCount, 2, MidpointRounding.AwayFromZero)
                : (decimal?)null;

            var locationSummary = string.Join(", ", new[] { merchant.City, merchant.Province }
                .Where(p => !string.IsNullOrWhiteSpace(p)));

            var canManageVisibility =
                merchant.Status == MerchantStatus.Active &&
                merchant.SellerVisibility != SellerVisibilityStatus.Blocked &&
                merchant.SellerVisibility != SellerVisibilityStatus.UnderReview;

            return new SellerAccountProfileDto
            {
                SellerId = ownerUserId.ToString(),
                MerchantId = merchant.Id,
                DisplayName = merchant.Name,
                TradingName = merchant.Name,
                Category = merchant.SellerCategory?.Name,
                LocationSummary = string.IsNullOrWhiteSpace(locationSummary) ? null : locationSummary,
                Bio = merchant.Description,
                City = merchant.City,
                Province = merchant.Province,
                PublicProfileImageUrl = merchant.ProfileImageUrl,
                ApprovalStatus = merchant.Status.ToString(),

                VisibilityStatus = MapVisibility(merchant.SellerVisibility),
                IsPaused = merchant.SellerVisibility == SellerVisibilityStatus.Paused,
                PauseReason = merchant.SellerPauseReason,
                PausedAtUtc = merchant.SellerPausedAtUtc,
                RequiresReview = merchant.SellerVisibility == SellerVisibilityStatus.UnderReview,
                ReviewReason = null,
                ReviewSubmittedAtUtc = null,

                // No real ZansiPulse trust score wired yet → honest null.
                SellerScore = null,
                SellerScoreBand = null,
                SellerRating = merchant.Rating,
                SellerReviewCount = merchant.ReviewCount,

                ContactEmail = merchant.ContactEmail,
                ContactPhoneNumber = merchant.ContactPhoneNumber,
                WhatsAppNumber = merchant.WhatsAppNumber,

                PickupAddressLine1 = merchant.AddressLine1,
                PickupCity = merchant.City,
                PickupProvince = merchant.Province,
                PickupPostalCode = merchant.PostalCode,

                ProductsCount = productsCount,
                ServicesCount = servicesCount,
                ShopCount = shopCount,
                ActiveListingsCount = active.Count,

                OrderStats = new SellerOrderStatsDto
                {
                    AcceptedCount = acceptedCount,
                    RejectedCount = rejectedCount,
                    CancelledCount = cancelledCount,
                    CompletionRate = completionRate,
                },
                Setup = new SellerSetupStatusDto
                {
                    HasVerifiedIdentity = merchant.KycStatus == MerchantKycStatus.Verified,
                    HasDocuments = merchant.KycStatus == MerchantKycStatus.Verified
                        || !string.IsNullOrWhiteSpace(merchant.IdNumber),
                    HasPickupAddress = !string.IsNullOrWhiteSpace(merchant.AddressLine1)
                        || !string.IsNullOrWhiteSpace(merchant.City),
                    HasContactDetails = !string.IsNullOrWhiteSpace(merchant.ContactEmail)
                        || !string.IsNullOrWhiteSpace(merchant.ContactPhoneNumber),
                },
                EditableSections = new SellerEditableSectionsDto
                {
                    TradingProfile = true,
                    Contact = true,
                    Verification = true,
                    Visibility = canManageVisibility,
                },
            };
        }

        private static string MapVisibility(SellerVisibilityStatus status) => status switch
        {
            SellerVisibilityStatus.Paused => "paused",
            SellerVisibilityStatus.UnderReview => "under_review",
            SellerVisibilityStatus.Blocked => "blocked",
            _ => "visible",
        };
    }
}
