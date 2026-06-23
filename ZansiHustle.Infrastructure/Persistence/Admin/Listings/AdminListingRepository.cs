using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZansiHustle.Application.Admin.Listings.Dtos;
using ZansiHustle.Application.Common.Paging;
using ZansiHustle.Application.Listings.Dtos;
using ZansiHustle.Application.Persistence.Admin.Listings;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Infrastructure.Persistence.Admin.Listings
{
    /// <summary>
    /// EF Core-backed queries for the admin Listings page.
    ///
    /// Deliberately applies NO visibility / status default / availability /
    /// source gates (unlike the buyer-facing <c>ListingRepository.SearchAsync</c>)
    /// — the admin grid shows EVERY listing across all merchants regardless of
    /// status, source, availability mode, or seller/shop visibility. Filters are
    /// only applied when the caller explicitly supplies them.
    /// </summary>
    public class AdminListingRepository : IAdminListingRepository
    {
        private readonly AppDbContext _context;

        public AdminListingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminListingsKpisDto> GetKpisAsync()
        {
            var listings = _context.Listings.AsNoTracking();

            var total = await listings.CountAsync();
            var products = await listings.CountAsync(l => l.Type == ListingType.Product);
            var services = await listings.CountAsync(l => l.Type == ListingType.Service);
            var active = await listings.CountAsync(l => l.Status == ListingStatus.Active);
            var draft = await listings.CountAsync(l => l.Status == ListingStatus.Draft);

            return new AdminListingsKpisDto
            {
                TotalListings = total,
                Products = products,
                Services = services,
                Active = active,
                Draft = draft,
            };
        }

        public async Task<PagedResult<ListingListItemDto>> GetPagedAsync(AdminListingQuery query)
        {
            var q = _context.Listings
                .AsNoTracking()
                .Include(l => l.Merchant)
                .Include(l => l.ShopProfile)
                .Include(l => l.SellerCategory)
                .AsQueryable();

            // Status filter — accept the lowercase enum-style strings the portal sends.
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                var mappedStatus = MapStatusFilter(query.Status);
                if (mappedStatus.HasValue)
                {
                    q = q.Where(l => l.Status == mappedStatus.Value);
                }
            }

            // Type filter — "product" / "service" ("all" / null = no filter).
            if (!string.IsNullOrWhiteSpace(query.Type))
            {
                var mappedType = MapTypeFilter(query.Type);
                if (mappedType.HasValue)
                {
                    q = q.Where(l => l.Type == mappedType.Value);
                }
            }

            // Source filter — "selleraccount" / "shopprofile" / "physicalstore".
            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                var mappedSource = MapSourceFilter(query.Source);
                if (mappedSource.HasValue)
                {
                    q = q.Where(l => l.ListingSource == mappedSource.Value);
                }
            }

            // Date range on listing creation.
            if (query.FromUtc.HasValue)
            {
                var from = DateTime.SpecifyKind(query.FromUtc.Value, DateTimeKind.Utc);
                q = q.Where(l => l.CreatedAtUtc >= from);
            }
            if (query.ToUtc.HasValue)
            {
                var to = DateTime.SpecifyKind(query.ToUtc.Value, DateTimeKind.Utc);
                q = q.Where(l => l.CreatedAtUtc <= to);
            }

            // Search across title, code, merchant name, and shop name.
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                q = q.Where(l =>
                    (l.Title != null && EF.Functions.Like(l.Title, $"%{term}%")) ||
                    (l.Code != null && EF.Functions.Like(l.Code, $"%{term}%")) ||
                    (l.Merchant != null && l.Merchant.Name != null && EF.Functions.Like(l.Merchant.Name, $"%{term}%")) ||
                    (l.ShopProfile != null && l.ShopProfile.Name != null && EF.Functions.Like(l.ShopProfile.Name, $"%{term}%"))
                );
            }

            var totalCount = await q.CountAsync();

            var skip = (query.Page - 1) * query.PageSize;
            var items = await q
                .OrderByDescending(l => l.CreatedAtUtc)
                .Skip(skip)
                .Take(query.PageSize)
                .Select(l => new ListingListItemDto
                {
                    Id = l.Id,
                    Slug = l.Slug,
                    Type = l.Type,
                    Status = l.Status,
                    AvailabilityMode = l.AvailabilityMode,
                    ListingSource = l.ListingSource,
                    ShopProfileId = l.ShopProfileId,
                    ShopProfileName = l.ShopProfile != null ? l.ShopProfile.Name : null,
                    ShopLogoUrl = l.ShopProfile != null ? l.ShopProfile.LogoUrl : null,
                    MerchantId = l.MerchantId,
                    MerchantName = l.Merchant != null ? l.Merchant.Name : null,
                    MerchantSlug = l.Merchant != null ? l.Merchant.Slug : null,
                    MerchantLogoUrl = l.Merchant != null ? l.Merchant.LogoUrl : null,
                    MerchantProfileImageUrl = l.Merchant != null ? l.Merchant.ProfileImageUrl : null,
                    MerchantVerified = l.Merchant != null && l.Merchant.KycStatus == MerchantKycStatus.Verified,
                    Title = l.Title,
                    Price = l.Price,
                    Currency = l.Currency,
                    SellerCategoryId = l.SellerCategoryId,
                    SellerCategoryName = l.SellerCategory != null ? l.SellerCategory.Name : null,
                    Province = l.Province,
                    City = l.City,
                    Images = l.Images,
                    IsFeatured = l.IsFeatured,
                    IsBoosted = l.IsBoosted,
                    Rating = l.Rating,
                    ReviewCount = l.ReviewCount,
                    Stock = l.Stock,
                    Condition = l.Condition,
                    PricingModel = l.PricingModel,
                    LikeCount = l.LikeCount,
                    CreatedAtUtc = l.CreatedAtUtc,
                })
                .ToListAsync();

            return new PagedResult<ListingListItemDto>
            {
                Items = items,
                Total = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
            };
        }

        /// <summary>Parses the portal's lowercase status filter into the ListingStatus enum.</summary>
        private static ListingStatus? MapStatusFilter(string status) => status.ToLowerInvariant() switch
        {
            "draft" => ListingStatus.Draft,
            "active" => ListingStatus.Active,
            "archived" => ListingStatus.Archived,
            "sold_out" => ListingStatus.SoldOut,
            "soldout" => ListingStatus.SoldOut,
            _ => (ListingStatus?)null,
        };

        /// <summary>Parses the portal's lowercase type filter into the ListingType enum.</summary>
        private static ListingType? MapTypeFilter(string type) => type.ToLowerInvariant() switch
        {
            "product" => ListingType.Product,
            "service" => ListingType.Service,
            _ => (ListingType?)null,
        };

        /// <summary>Parses the portal's lowercase source filter into the ListingSource enum.</summary>
        private static ListingSource? MapSourceFilter(string source) => source.ToLowerInvariant() switch
        {
            "selleraccount" => ListingSource.SellerAccount,
            "shopprofile" => ListingSource.ShopProfile,
            "physicalstore" => ListingSource.PhysicalStore,
            _ => (ListingSource?)null,
        };
    }
}
