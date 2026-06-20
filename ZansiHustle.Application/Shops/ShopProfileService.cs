using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.SellerCategories;
using ZansiHustle.Application.Persistence.Shops;
using ZansiHustle.Application.Persistence.Users;
using ZansiHustle.Application.Shops.Dtos;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Shops;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Shops
{
    /// <summary>
    /// Lifecycle + presentation operations for <see cref="ShopProfile"/>.
    /// Decoupled from <c>IMerchantService</c>: this service never
    /// mutates merchant rows directly. The merchant is consulted only
    /// to verify ownership and Active status at create time.
    ///
    /// Early-access policy (changeable later): newly-created shops
    /// auto-activate with <c>Status=Active</c> +
    /// <c>SubscriptionStatus=EarlyAccess</c>. When billing ships, this
    /// can be changed to Status=PendingReview / Trial without touching
    /// the rest of the surface area.
    /// </summary>
    public class ShopProfileService : IShopProfileService
    {
        private readonly IShopProfileRepository _shopRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ISellerCategoryRepository _categoryRepository;
        private readonly IUserRepository _userRepository;

        public ShopProfileService(
            IShopProfileRepository shopRepository,
            IMerchantRepository merchantRepository,
            ISellerCategoryRepository categoryRepository,
            IUserRepository userRepository)
        {
            _shopRepository = shopRepository;
            _merchantRepository = merchantRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
        }

        // ─── Mine (owner-facing) ────────────────────────────────────

        public async Task<Result<ShopProfileDto?>> GetMineAsync(Guid ownerUserId)
        {
            try
            {
                var shop = await _shopRepository.GetMineAsync(ownerUserId);
                if (shop is null)
                {
                    // Explicit success-with-null. The HTTP layer in
                    // BaseController serialises this as { success: true,
                    // data: null }, which the app reads as "no shop yet"
                    // — the empty-state path on My Shop.
                    return Result<ShopProfileDto?>.Success(null);
                }

                var dto = await MapToDtoAsync(shop);
                return Result<ShopProfileDto?>.Success(dto);
            }
            catch (Exception ex)
            {
                return Result<ShopProfileDto?>.Failure(
                    ErrorCodes.Exception,
                    $"An error occurred while loading your shop. {ex.Message}");
            }
        }

        public async Task<Result<ShopProfileDto>> CreateMineAsync(Guid ownerUserId, CreateShopRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.BadRequest, "Request is required.");
                if (string.IsNullOrWhiteSpace(request.Name))
                    return Result<ShopProfileDto>.Failure(ErrorCodes.BadRequest, "Shop name is required.");

                // Caller must own an Active OnlineStore merchant. Sellers
                // who are still pending approval cannot open a shop —
                // the storefront has no meaning until the merchant
                // record is approved to sell.
                var merchants = await _merchantRepository.GetByOwnerAsync(ownerUserId);
                var onlineMerchant = merchants.FirstOrDefault(m => m.Type == MerchantType.OnlineStore);
                if (onlineMerchant is null)
                {
                    return Result<ShopProfileDto>.Failure(
                        ErrorCodes.Forbidden,
                        "You need an approved online-seller account before opening a shop.");
                }
                if (onlineMerchant.Status != MerchantStatus.Active)
                {
                    return Result<ShopProfileDto>.Failure(
                        ErrorCodes.Forbidden,
                        "Your seller account is not yet active. Wait for approval before opening a shop.");
                }

                // Duplicate-active guard: one non-Suspended shop per
                // merchant for now. The filtered unique index is the
                // backstop, but a service-layer check returns a clean
                // 409 with a friendly message instead of a DB error.
                var existing = await _shopRepository.GetActiveByMerchantAsync(onlineMerchant.Id);
                if (existing is not null)
                {
                    return Result<ShopProfileDto>.Failure(
                        ErrorCodes.Conflict,
                        "You already have a shop. Edit it instead of creating another.");
                }

                var categoryCheck = await ValidateCategoriesAsync(request.SellerCategoryId, request.SellerSubcategoryId);
                if (!categoryCheck.IsSuccess)
                    return Result<ShopProfileDto>.Failure(categoryCheck.Code, categoryCheck.Message);

                // Theme preset: null/empty → default; unknown → 400 (never save junk).
                var themeKey = ShopThemePresets.Normalize(request.ThemePresetKey);
                if (themeKey is null)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.BadRequest, "Selected shop theme is not recognised.");

                // Background mode: null/empty → light; unknown → 400.
                var backgroundMode = ShopThemeBackgroundModes.Normalize(request.ThemeBackgroundMode);
                if (backgroundMode is null)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.BadRequest, "Selected background style is not recognised.");

                var now = DateTime.UtcNow;
                var shop = new ShopProfile
                {
                    Id = Guid.NewGuid(),
                    MerchantId = onlineMerchant.Id,
                    Slug = await GenerateUniqueSlugAsync(request.Name),
                    Name = request.Name.Trim(),
                    Description = Trim(request.Description),
                    LogoUrl = Trim(request.LogoUrl),
                    BannerUrl = Trim(request.BannerUrl),
                    ContactEmail = Trim(request.ContactEmail),
                    ContactPhoneNumber = Trim(request.ContactPhoneNumber),
                    WhatsAppNumber = Trim(request.WhatsAppNumber),
                    SellerCategoryId = request.SellerCategoryId,
                    SellerSubcategoryId = request.SellerSubcategoryId,
                    Province = Trim(request.Province),
                    City = Trim(request.City),
                    AddressLine1 = Trim(request.AddressLine1),
                    ThemePresetKey = themeKey,
                    ThemeBackgroundMode = backgroundMode,
                    // Early-access default: skip admin review, mark
                    // Active immediately, tag the subscription state
                    // as EarlyAccess so reporting can count opt-ins
                    // before any paid plan ships.
                    Status = ShopProfileStatus.Active,
                    SubscriptionStatus = ShopSubscriptionStatus.EarlyAccess,
                    EarlyAccessGrantedAtUtc = now,
                    ActivatedAtUtc = now,
                    CreatedAtUtc = now,
                };

                await _shopRepository.AddAsync(shop);
                if (!await _shopRepository.SaveChangesAsync())
                    return Result<ShopProfileDto>.Failure(ErrorCodes.Exception, "Failed to create shop.");

                var dto = await MapToDtoAsync(shop);
                return Result<ShopProfileDto>.Success(dto, "Shop created.");
            }
            catch (Exception ex)
            {
                return Result<ShopProfileDto>.Failure(
                    ErrorCodes.Exception,
                    $"An error occurred while creating your shop. {ex.Message}");
            }
        }

        public async Task<Result<ShopProfileDto>> UpdateMineAsync(Guid ownerUserId, Guid shopId, UpdateShopRequestDto request)
        {
            try
            {
                if (request is null)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                var shop = await _shopRepository.GetByIdAsync(shopId);
                if (shop is null)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.NotFound, "Shop not found.");

                // Ownership check via Merchant.OwnerUserId.
                var merchant = await _merchantRepository.GetByIdAsync(shop.MerchantId);
                if (merchant is null || merchant.OwnerUserId != ownerUserId)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.Forbidden, "You don't have access to this shop.");

                if (request.SellerCategoryId.HasValue || request.SellerSubcategoryId.HasValue)
                {
                    var categoryCheck = await ValidateCategoriesAsync(
                        request.SellerCategoryId ?? shop.SellerCategoryId,
                        request.SellerSubcategoryId ?? shop.SellerSubcategoryId);
                    if (!categoryCheck.IsSuccess)
                        return Result<ShopProfileDto>.Failure(categoryCheck.Code, categoryCheck.Message);
                }

                // PATCH semantics: only supplied fields are applied.
                // `Name` is special — non-empty replaces; empty/null is
                // ignored (we never blank a required column). Slug is
                // immutable after creation to keep external links stable.
                if (!string.IsNullOrWhiteSpace(request.Name)) shop.Name = request.Name.Trim();
                if (request.Description != null) shop.Description = Trim(request.Description);
                if (request.LogoUrl != null) shop.LogoUrl = Trim(request.LogoUrl);
                if (request.BannerUrl != null) shop.BannerUrl = Trim(request.BannerUrl);
                if (request.ContactEmail != null) shop.ContactEmail = Trim(request.ContactEmail);
                if (request.ContactPhoneNumber != null) shop.ContactPhoneNumber = Trim(request.ContactPhoneNumber);
                if (request.WhatsAppNumber != null) shop.WhatsAppNumber = Trim(request.WhatsAppNumber);
                if (request.SellerCategoryId.HasValue) shop.SellerCategoryId = request.SellerCategoryId;
                if (request.SellerSubcategoryId.HasValue) shop.SellerSubcategoryId = request.SellerSubcategoryId;
                if (request.Province != null) shop.Province = Trim(request.Province);
                if (request.City != null) shop.City = Trim(request.City);
                if (request.AddressLine1 != null) shop.AddressLine1 = Trim(request.AddressLine1);
                // Theme: only when supplied. Unknown value → 400; empty → default.
                if (request.ThemePresetKey != null)
                {
                    var themeKey = ShopThemePresets.Normalize(request.ThemePresetKey);
                    if (themeKey is null)
                        return Result<ShopProfileDto>.Failure(ErrorCodes.BadRequest, "Selected shop theme is not recognised.");
                    shop.ThemePresetKey = themeKey;
                }
                // Background mode: only when supplied. Unknown value → 400; empty → light.
                if (request.ThemeBackgroundMode != null)
                {
                    var backgroundMode = ShopThemeBackgroundModes.Normalize(request.ThemeBackgroundMode);
                    if (backgroundMode is null)
                        return Result<ShopProfileDto>.Failure(ErrorCodes.BadRequest, "Selected background style is not recognised.");
                    shop.ThemeBackgroundMode = backgroundMode;
                }
                shop.UpdatedAtUtc = DateTime.UtcNow;

                _shopRepository.Update(shop);
                if (!await _shopRepository.SaveChangesAsync())
                    return Result<ShopProfileDto>.Failure(ErrorCodes.Exception, "Failed to update shop.");

                var dto = await MapToDtoAsync(shop);
                return Result<ShopProfileDto>.Success(dto, "Shop updated.");
            }
            catch (Exception ex)
            {
                return Result<ShopProfileDto>.Failure(
                    ErrorCodes.Exception,
                    $"An error occurred while updating your shop. {ex.Message}");
            }
        }

        // ─── Public ─────────────────────────────────────────────────

        public async Task<Result<ShopProfilePublicDto>> GetPublicByIdAsync(Guid id)
        {
            try
            {
                var shop = await _shopRepository.GetByIdAsync(id);
                // Hide Suspended / Draft / PendingReview from public
                // discovery — the existence of those states is an
                // owner/admin concern, not a buyer concern.
                if (shop is null || shop.Status != ShopProfileStatus.Active)
                    return Result<ShopProfilePublicDto>.Failure(ErrorCodes.NotFound, "Shop not found.");

                // Detail endpoint uses the enriched projection (joins
                // Merchant + owner User for the About-tab fields). The
                // list endpoint stays on the lean projection — paging
                // through 100 shops shouldn't pay N+1 join cost for
                // data the cards don't render.
                return Result<ShopProfilePublicDto>.Success(await MapToPublicDetailDtoAsync(shop));
            }
            catch (Exception ex)
            {
                return Result<ShopProfilePublicDto>.Failure(
                    ErrorCodes.Exception,
                    $"An error occurred while loading the shop. {ex.Message}");
            }
        }

        public async Task<Result<PagedResult<ShopProfilePublicDto>>> SearchPublicAsync(int page, int pageSize, string? q)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var paged = await _shopRepository.SearchPublicAsync(page, pageSize, q);

                var items = new System.Collections.Generic.List<ShopProfilePublicDto>(paged.Items.Count);
                foreach (var shop in paged.Items)
                    items.Add(await MapToPublicDtoAsync(shop));

                return Result<PagedResult<ShopProfilePublicDto>>.Success(new PagedResult<ShopProfilePublicDto>
                {
                    Items = items,
                    Total = paged.Total,
                    Page = paged.Page,
                    PageSize = paged.PageSize,
                });
            }
            catch (Exception ex)
            {
                return Result<PagedResult<ShopProfilePublicDto>>.Failure(
                    ErrorCodes.Exception,
                    $"An error occurred while listing shops. {ex.Message}");
            }
        }

        // ─── Admin ──────────────────────────────────────────────────

        public async Task<Result<PagedResult<ShopProfileDto>>> SearchAdminAsync(int page, int pageSize, ShopProfileStatus? status, string? q)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var paged = await _shopRepository.SearchAdminAsync(page, pageSize, status, q);

                var items = new System.Collections.Generic.List<ShopProfileDto>(paged.Items.Count);
                foreach (var shop in paged.Items)
                    items.Add(await MapToDtoAsync(shop));

                return Result<PagedResult<ShopProfileDto>>.Success(new PagedResult<ShopProfileDto>
                {
                    Items = items,
                    Total = paged.Total,
                    Page = paged.Page,
                    PageSize = paged.PageSize,
                });
            }
            catch (Exception ex)
            {
                return Result<PagedResult<ShopProfileDto>>.Failure(
                    ErrorCodes.Exception,
                    $"An error occurred while listing shops. {ex.Message}");
            }
        }

        public async Task<Result<ShopProfileDto>> SuspendAsync(Guid id, string? reason)
        {
            try
            {
                var shop = await _shopRepository.GetByIdAsync(id);
                if (shop is null)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.NotFound, "Shop not found.");

                shop.Status = ShopProfileStatus.Suspended;
                shop.SuspendedAtUtc = DateTime.UtcNow;
                shop.SuspensionReason = Trim(reason);
                shop.UpdatedAtUtc = DateTime.UtcNow;

                _shopRepository.Update(shop);
                if (!await _shopRepository.SaveChangesAsync())
                    return Result<ShopProfileDto>.Failure(ErrorCodes.Exception, "Failed to suspend shop.");

                return Result<ShopProfileDto>.Success(await MapToDtoAsync(shop), "Shop suspended.");
            }
            catch (Exception ex)
            {
                return Result<ShopProfileDto>.Failure(
                    ErrorCodes.Exception,
                    $"An error occurred while suspending the shop. {ex.Message}");
            }
        }

        public async Task<Result<ShopProfileDto>> ReactivateAsync(Guid id)
        {
            try
            {
                var shop = await _shopRepository.GetByIdAsync(id);
                if (shop is null)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.NotFound, "Shop not found.");

                // Re-activate only allowed from Suspended — re-activating
                // an Active shop is a no-op confused call and we'd rather
                // be loud about it than silently succeed.
                if (shop.Status != ShopProfileStatus.Suspended)
                    return Result<ShopProfileDto>.Failure(ErrorCodes.BadRequest, "Only suspended shops can be reactivated.");

                shop.Status = ShopProfileStatus.Active;
                shop.SuspendedAtUtc = null;
                shop.SuspensionReason = null;
                shop.ActivatedAtUtc = DateTime.UtcNow;
                shop.UpdatedAtUtc = DateTime.UtcNow;

                _shopRepository.Update(shop);
                if (!await _shopRepository.SaveChangesAsync())
                    return Result<ShopProfileDto>.Failure(ErrorCodes.Exception, "Failed to reactivate shop.");

                return Result<ShopProfileDto>.Success(await MapToDtoAsync(shop), "Shop reactivated.");
            }
            catch (Exception ex)
            {
                return Result<ShopProfileDto>.Failure(
                    ErrorCodes.Exception,
                    $"An error occurred while reactivating the shop. {ex.Message}");
            }
        }

        // ─── Helpers ────────────────────────────────────────────────

        private async Task<ShopProfileDto> MapToDtoAsync(ShopProfile s)
        {
            string? categoryName = null;
            string? subcategoryName = null;
            if (s.SellerCategoryId.HasValue)
            {
                var cat = await _categoryRepository.GetByIdAsync(s.SellerCategoryId.Value);
                categoryName = cat?.Name;
            }
            if (s.SellerSubcategoryId.HasValue)
            {
                var sub = await _categoryRepository.GetSubcategoryByIdAsync(s.SellerSubcategoryId.Value);
                subcategoryName = sub?.Name;
            }

            return new ShopProfileDto
            {
                Id = s.Id,
                MerchantId = s.MerchantId,
                Slug = s.Slug,
                Name = s.Name,
                Description = s.Description,
                LogoUrl = s.LogoUrl,
                BannerUrl = s.BannerUrl,
                ThemePresetKey = s.ThemePresetKey,
                ThemeBackgroundMode = s.ThemeBackgroundMode,
                ContactEmail = s.ContactEmail,
                ContactPhoneNumber = s.ContactPhoneNumber,
                WhatsAppNumber = s.WhatsAppNumber,
                SellerCategoryId = s.SellerCategoryId,
                SellerCategoryName = categoryName,
                SellerSubcategoryId = s.SellerSubcategoryId,
                SellerSubcategoryName = subcategoryName,
                Province = s.Province,
                City = s.City,
                AddressLine1 = s.AddressLine1,
                Status = s.Status,
                SubscriptionStatus = s.SubscriptionStatus,
                EarlyAccessGrantedAtUtc = s.EarlyAccessGrantedAtUtc,
                EarlyAccessUntilUtc = s.EarlyAccessUntilUtc,
                SubscriptionStartedAtUtc = s.SubscriptionStartedAtUtc,
                SubscriptionEndsAtUtc = s.SubscriptionEndsAtUtc,
                BillingProvider = s.BillingProvider,
                ActivatedAtUtc = s.ActivatedAtUtc,
                SuspendedAtUtc = s.SuspendedAtUtc,
                SuspensionReason = s.SuspensionReason,
                Rating = s.Rating,
                ReviewCount = s.ReviewCount,
                CreatedAtUtc = s.CreatedAtUtc,
                UpdatedAtUtc = s.UpdatedAtUtc,
            };
        }

        private async Task<ShopProfilePublicDto> MapToPublicDtoAsync(ShopProfile s)
        {
            string? categoryName = null;
            string? subcategoryName = null;
            if (s.SellerCategoryId.HasValue)
            {
                var cat = await _categoryRepository.GetByIdAsync(s.SellerCategoryId.Value);
                categoryName = cat?.Name;
            }
            if (s.SellerSubcategoryId.HasValue)
            {
                var sub = await _categoryRepository.GetSubcategoryByIdAsync(s.SellerSubcategoryId.Value);
                subcategoryName = sub?.Name;
            }

            return new ShopProfilePublicDto
            {
                Id = s.Id,
                MerchantId = s.MerchantId,
                Slug = s.Slug,
                Name = s.Name,
                Description = s.Description,
                LogoUrl = s.LogoUrl,
                BannerUrl = s.BannerUrl,
                ThemePresetKey = s.ThemePresetKey,
                ThemeBackgroundMode = s.ThemeBackgroundMode,
                SellerCategoryName = categoryName,
                SellerSubcategoryName = subcategoryName,
                Province = s.Province,
                City = s.City,
                Rating = s.Rating,
                ReviewCount = s.ReviewCount,
                CreatedAtUtc = s.CreatedAtUtc,
                // OwnerDisplayName / WebsiteUrl / IsVerified are
                // intentionally left at their defaults on the list path.
                // The detail-only enrichment lives in
                // MapToPublicDetailDtoAsync.
            };
        }

        /// <summary>
        /// Enriched public projection used by the shop-detail endpoint.
        /// Adds owner display name, the merchant's website, and a KYC-
        /// derived verified flag on top of the lean list mapping.
        ///
        /// Failures of the merchant / user join are swallowed: the About
        /// tab simply hides whatever row's data is missing rather than
        /// failing the whole detail load. Shop data corruption (e.g.
        /// orphan ShopProfile with no Merchant) should not surface as a
        /// 500 to a buyer trying to view the page.
        /// </summary>
        private async Task<ShopProfilePublicDto> MapToPublicDetailDtoAsync(ShopProfile s)
        {
            var dto = await MapToPublicDtoAsync(s);

            var merchant = await _merchantRepository.GetByIdAsync(s.MerchantId);
            if (merchant is null) return dto;

            dto.WebsiteUrl = string.IsNullOrWhiteSpace(merchant.WebsiteUrl) ? null : merchant.WebsiteUrl;
            dto.IsVerified = merchant.KycStatus == MerchantKycStatus.Verified;

            if (merchant.OwnerUserId.HasValue && merchant.OwnerUserId.Value != Guid.Empty)
            {
                var owner = await _userRepository.GetByIdAsync(merchant.OwnerUserId.Value);
                if (owner != null)
                {
                    // Same display-name formula the Reviews surface uses
                    // for `ReviewerDisplayName` — keeps both surfaces in
                    // lockstep so the owner shows up consistently on
                    // their own shop's About tab and on any reviews they
                    // leave elsewhere. Null when the user record has no
                    // populated name (the About row is then hidden).
                    var name = $"{owner.FirstName} {owner.LastName}".Trim();
                    dto.OwnerDisplayName = string.IsNullOrWhiteSpace(name) ? null : name;
                }
            }

            return dto;
        }

        private async Task<Result> ValidateCategoriesAsync(Guid? categoryId, Guid? subcategoryId)
        {
            if (categoryId.HasValue)
            {
                var cat = await _categoryRepository.GetByIdAsync(categoryId.Value);
                if (cat is null)
                    return Result.Failure(ErrorCodes.BadRequest, "Selected category was not found.");
            }
            if (subcategoryId.HasValue)
            {
                var sub = await _categoryRepository.GetSubcategoryByIdAsync(subcategoryId.Value);
                if (sub is null)
                    return Result.Failure(ErrorCodes.BadRequest, "Selected subcategory was not found.");
                if (categoryId.HasValue && sub.SellerCategoryId != categoryId.Value)
                    return Result.Failure(ErrorCodes.BadRequest, "Subcategory does not belong to the chosen category.");
            }
            return Result.Success();
        }

        private async Task<string> GenerateUniqueSlugAsync(string name)
        {
            var baseSlug = Slugify(name);
            if (string.IsNullOrEmpty(baseSlug)) baseSlug = "shop";

            var candidate = baseSlug;
            var suffix = 2;
            while (await _shopRepository.SlugExistsAsync(candidate))
            {
                candidate = $"{baseSlug}-{suffix}";
                suffix++;
                if (suffix > 9999)
                {
                    candidate = $"{baseSlug}-{Guid.NewGuid().ToString("N")[..6]}";
                    break;
                }
            }
            return candidate;
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var sb = new StringBuilder(value.Length);
            var lastWasHyphen = false;
            foreach (var ch in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                    lastWasHyphen = false;
                }
                else if (!lastWasHyphen && sb.Length > 0)
                {
                    sb.Append('-');
                    lastWasHyphen = true;
                }
            }
            var slug = sb.ToString().Trim('-');
            return slug.Length > 80 ? slug[..80] : slug;
        }

        private static string? Trim(string? value)
        {
            if (value is null) return null;
            var trimmed = value.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }
}
