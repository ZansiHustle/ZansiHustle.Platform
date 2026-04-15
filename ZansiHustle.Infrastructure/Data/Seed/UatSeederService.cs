using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Admin.Seeding;
using ZansiHustle.Application.Persistence.Listings;
using ZansiHustle.Application.Persistence.Merchants;
using ZansiHustle.Application.Persistence.Orders;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Orders;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Orders;
using ZansiHustle.Shared.Enums.User;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Data.Seed
{
    /// <summary>
    /// Idempotent UAT/dev data seeder. Creates users through <see cref="UserManager{TUser}"/>
    /// so real Identity flows apply (password hashing, normalised email, role mapping);
    /// creates merchants/listings/orders directly via repositories using stable codes/slugs
    /// so re-runs skip existing records.
    /// </summary>
    public sealed class UatSeederService : IUatSeederService
    {
        private readonly UserManager<User> _userManager;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<UatSeederService> _logger;

        public UatSeederService(UserManager<User> userManager, IMerchantRepository merchantRepository, IListingRepository listingRepository, IOrderRepository orderRepository, ILogger<UatSeederService> logger)
        {
            _userManager = userManager;
            _merchantRepository = merchantRepository;
            _listingRepository = listingRepository;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Result<UatSeedSummaryDto>> SeedAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var summary = new UatSeedSummaryDto
                {
                    Password = UatSeedContent.UatPassword,
                    SellerEmails = UatSeedContent.Sellers.Select(s => s.Email).ToList(),
                    BuyerEmails = UatSeedContent.Buyers.Select(b => b.Email).ToList()
                };

                var sellersByEmail = await SeedSellersAsync(summary, cancellationToken);
                await SeedBuyersAsync(summary, cancellationToken);

                var merchantsByShopIndex = await SeedMerchantsAsync(sellersByEmail, summary, cancellationToken);
                var listingsByKey = await SeedListingsAsync(merchantsByShopIndex, summary, cancellationToken);
                await SeedOrdersAsync(merchantsByShopIndex, listingsByKey, summary, cancellationToken);

                return Result<UatSeedSummaryDto>.Success(summary, "UAT seed completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UAT seed failed.");
                return Result<UatSeedSummaryDto>.Failure(ErrorCodes.Exception, $"UAT seed failed. {ex.Message}");
            }
        }

        // ─── Users ───────────────────────────────────────────────────────────

        private async Task<Dictionary<string, User>> SeedSellersAsync(UatSeedSummaryDto summary, CancellationToken ct)
        {
            var lookup = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase);

            foreach (var s in UatSeedContent.Sellers)
            {
                ct.ThrowIfCancellationRequested();

                var existing = await _userManager.FindByEmailAsync(s.Email);

                if (existing != null)
                {
                    lookup[s.Email] = existing;
                    summary.SkippedUsers++;
                    continue;
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = s.Email,
                    UserName = s.Email,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    PhoneNumber = s.Phone,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    AccountStatus = AccountStatus.Active,
                    CreatedOnUtc = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, UatSeedContent.UatPassword);

                if (!createResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create seller {s.Email}: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");

                var roleResult = await _userManager.AddToRolesAsync(user, new[]
                {
                    nameof(UserRole.Seller),
                    nameof(UserRole.Merchant)
                });

                if (!roleResult.Succeeded)
                    throw new InvalidOperationException($"Failed to assign roles to {s.Email}: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");

                lookup[s.Email] = user;
                summary.CreatedUsers++;
                _logger.LogInformation("UAT seed: created seller {Email}.", s.Email);
            }

            return lookup;
        }

        private async Task SeedBuyersAsync(UatSeedSummaryDto summary, CancellationToken ct)
        {
            foreach (var b in UatSeedContent.Buyers)
            {
                ct.ThrowIfCancellationRequested();

                var existing = await _userManager.FindByEmailAsync(b.Email);

                if (existing != null)
                {
                    summary.SkippedUsers++;
                    continue;
                }

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = b.Email,
                    UserName = b.Email,
                    FirstName = b.FirstName,
                    LastName = b.LastName,
                    PhoneNumber = b.Phone,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    IsActive = true,
                    AccountStatus = AccountStatus.Active,
                    CreatedOnUtc = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, UatSeedContent.UatPassword);

                if (!createResult.Succeeded)
                    throw new InvalidOperationException($"Failed to create buyer {b.Email}: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");

                var roleResult = await _userManager.AddToRolesAsync(user, new[] { nameof(UserRole.Customer) });

                if (!roleResult.Succeeded)
                    throw new InvalidOperationException($"Failed to assign role to {b.Email}: {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");

                summary.CreatedUsers++;
                _logger.LogInformation("UAT seed: created buyer {Email}.", b.Email);
            }
        }

        // ─── Merchants ───────────────────────────────────────────────────────

        private async Task<Dictionary<int, Merchant>> SeedMerchantsAsync(Dictionary<string, User> sellersByEmail, UatSeedSummaryDto summary, CancellationToken ct)
        {
            var merchants = new Dictionary<int, Merchant>();

            for (var i = 0; i < UatSeedContent.Sellers.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var seller = UatSeedContent.Sellers[i];
                var shopIndex = i + 1;
                var slug = Slugify(seller.ShopName);

                var existing = await _merchantRepository.GetBySlugAsync(slug);

                if (existing != null)
                {
                    merchants[shopIndex] = existing;
                    summary.SkippedMerchants++;
                    summary.Merchants.Add(existing.Name);
                    continue;
                }

                var ownerUser = sellersByEmail[seller.Email];

                var merchant = new Merchant
                {
                    Id = Guid.NewGuid(),
                    Code = $"MER-SEED-{shopIndex:D2}",
                    Slug = slug,
                    Name = seller.ShopName,
                    Description = seller.ShopDescription,
                    Type = MerchantType.OnlineStore,
                    Status = MerchantStatus.Active,
                    KycStatus = MerchantKycStatus.Verified,
                    IsPayoutEligible = true,
                    OwnerUserId = ownerUser.Id,
                    SellerCategoryId = seller.CategoryId,
                    ContactEmail = seller.Email,
                    ContactPhoneNumber = seller.Phone,
                    Province = seller.Province,
                    City = seller.City,
                    LogoUrl = seller.LogoUrl,
                    FollowersCount = 0,
                    Rating = 4.5m,
                    ReviewCount = 0,
                    TotalOrders = 0,
                    TotalRevenue = 0m,
                    CreatedAtUtc = DateTime.UtcNow
                };

                await _merchantRepository.AddAsync(merchant);
                await _merchantRepository.SaveChangesAsync();

                merchants[shopIndex] = merchant;
                summary.CreatedMerchants++;
                summary.Merchants.Add(merchant.Name);
                _logger.LogInformation("UAT seed: created merchant {Name}.", merchant.Name);
            }

            return merchants;
        }

        // ─── Listings ────────────────────────────────────────────────────────

        /// <summary>
        /// Key format: "P-{shopIdx}-{productIdx}" or "S-{shopIdx}-{serviceIdx}"
        /// where the index is 0-based within that shop's ordered list.
        /// </summary>
        private async Task<Dictionary<string, Listing>> SeedListingsAsync(Dictionary<int, Merchant> merchantsByShopIndex, UatSeedSummaryDto summary, CancellationToken ct)
        {
            var result = new Dictionary<string, Listing>();

            // Products grouped by shop, preserving source order so SeedOrders can reference by index.
            var productsByShop = UatSeedContent.Products
                .GroupBy(p => p.ShopIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (shopIndex, items) in productsByShop)
            {
                if (!merchantsByShopIndex.TryGetValue(shopIndex, out var merchant)) continue;

                for (var i = 0; i < items.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var p = items[i];
                    var slug = UniqueSlug($"{Slugify(p.Title)}-{merchant.Slug}-{i}");
                    var key = $"P-{shopIndex}-{i}";

                    var existing = await _listingRepository.GetBySlugAsync(slug);

                    if (existing != null)
                    {
                        result[key] = existing;
                        summary.SkippedListings++;
                        continue;
                    }

                    var listing = new Listing
                    {
                        Id = Guid.NewGuid(),
                        Code = $"LIS-SEED-P-{shopIndex:D2}-{i:D2}",
                        Slug = slug,
                        Type = ListingType.Product,
                        Status = ListingStatus.Active,
                        MerchantId = merchant.Id,
                        Title = p.Title,
                        Description = p.Description,
                        Price = p.Price,
                        Currency = "ZAR",
                        SellerCategoryId = p.CategoryId,
                        Province = merchant.Province,
                        City = merchant.City,
                        Images = p.Images.ToList(),
                        IsFeatured = i == 0, // first item per shop is featured for visibility
                        IsBoosted = false,
                        Rating = null,
                        ReviewCount = 0,
                        Stock = p.Stock,
                        Condition = p.Condition,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    await _listingRepository.AddAsync(listing);
                    await _listingRepository.SaveChangesAsync();

                    result[key] = listing;
                    summary.CreatedListings++;
                }
            }

            var servicesByShop = UatSeedContent.Services
                .GroupBy(s => s.ShopIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (shopIndex, items) in servicesByShop)
            {
                if (!merchantsByShopIndex.TryGetValue(shopIndex, out var merchant)) continue;

                for (var i = 0; i < items.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var sv = items[i];
                    var slug = UniqueSlug($"{Slugify(sv.Title)}-{merchant.Slug}-svc-{i}");
                    var key = $"S-{shopIndex}-{i}";

                    var existing = await _listingRepository.GetBySlugAsync(slug);

                    if (existing != null)
                    {
                        result[key] = existing;
                        summary.SkippedListings++;
                        continue;
                    }

                    var listing = new Listing
                    {
                        Id = Guid.NewGuid(),
                        Code = $"LIS-SEED-S-{shopIndex:D2}-{i:D2}",
                        Slug = slug,
                        Type = ListingType.Service,
                        Status = ListingStatus.Active,
                        MerchantId = merchant.Id,
                        Title = sv.Title,
                        Description = sv.Description,
                        Price = sv.Price,
                        Currency = "ZAR",
                        SellerCategoryId = sv.CategoryId,
                        Province = merchant.Province,
                        City = merchant.City,
                        Images = sv.Images.ToList(),
                        IsFeatured = i == 0,
                        IsBoosted = false,
                        Rating = null,
                        ReviewCount = 0,
                        PricingModel = sv.PricingModel,
                        ServiceArea = sv.ServiceArea,
                        Availability = sv.Availability.ToList(),
                        BookingMethods = sv.BookingMethods.ToList(),
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    await _listingRepository.AddAsync(listing);
                    await _listingRepository.SaveChangesAsync();

                    result[key] = listing;
                    summary.CreatedListings++;
                }
            }

            return result;
        }

        // ─── Orders ──────────────────────────────────────────────────────────

        private async Task SeedOrdersAsync(Dictionary<int, Merchant> merchantsByShopIndex, Dictionary<string, Listing> listingsByKey, UatSeedSummaryDto summary, CancellationToken ct)
        {
            // Pre-load buyers by email for fast lookup.
            var buyersByIndex = new Dictionary<int, User>();
            for (var i = 0; i < UatSeedContent.Buyers.Count; i++)
            {
                var buyer = await _userManager.FindByEmailAsync(UatSeedContent.Buyers[i].Email);

                if (buyer != null)
                    buyersByIndex[i + 1] = buyer;
            }

            for (var i = 0; i < UatSeedContent.Orders.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var spec = UatSeedContent.Orders[i];
                var code = $"ORD-SEED-{i + 1:D3}";

                if (await _orderRepository.ExistsByCodeAsync(code))
                {
                    summary.SkippedOrders++;
                    continue;
                }

                if (!buyersByIndex.TryGetValue(spec.BuyerIndex, out var buyer)) continue;
                if (!merchantsByShopIndex.TryGetValue(spec.ShopIndex, out var merchant)) continue;

                var key = $"{(spec.IsService ? "S" : "P")}-{spec.ShopIndex}-{spec.ListingIndex}";
                if (!listingsByKey.TryGetValue(key, out var listing)) continue;

                var unitPrice = listing.Price;
                var lineTotal = unitPrice * spec.Quantity;
                var createdAt = DateTime.UtcNow.AddDays(-spec.CreatedDaysAgo);

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    BuyerUserId = buyer.Id,
                    BuyerName = $"{buyer.FirstName} {buyer.LastName}".Trim(),
                    BuyerEmail = buyer.Email,
                    BuyerPhone = buyer.PhoneNumber,
                    MerchantId = merchant.Id,
                    Status = spec.Status,
                    PaymentStatus = PaymentStatus.Pending,
                    Subtotal = lineTotal,
                    Total = lineTotal,
                    Currency = "ZAR",
                    DeliveryAddress = spec.DeliveryAddress,
                    Notes = spec.Notes,
                    CreatedAtUtc = createdAt
                };

                switch (spec.Status)
                {
                    case OrderStatus.Confirmed:
                        order.ConfirmedAtUtc = createdAt.AddHours(6);
                        order.UpdatedAtUtc = order.ConfirmedAtUtc;
                        break;
                    case OrderStatus.InProgress:
                        order.ConfirmedAtUtc = createdAt.AddHours(6);
                        order.UpdatedAtUtc = createdAt.AddHours(24);
                        break;
                    case OrderStatus.Completed:
                        order.ConfirmedAtUtc = createdAt.AddHours(6);
                        order.CompletedAtUtc = createdAt.AddDays(2);
                        order.UpdatedAtUtc = order.CompletedAtUtc;
                        break;
                    case OrderStatus.Cancelled:
                        order.CancelledAtUtc = createdAt.AddHours(12);
                        order.CancellationReason = spec.Notes;
                        order.UpdatedAtUtc = order.CancelledAtUtc;
                        break;
                }

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ListingId = listing.Id,
                    ListingType = listing.Type,
                    TitleSnapshot = listing.Title,
                    ImageSnapshot = listing.Images.FirstOrDefault(),
                    UnitPrice = unitPrice,
                    Quantity = spec.Quantity,
                    LineTotal = lineTotal,
                    CreatedAtUtc = createdAt
                });

                await _orderRepository.AddAsync(order);
                await _orderRepository.SaveChangesAsync();

                summary.CreatedOrders++;
                _logger.LogInformation("UAT seed: created order {Code} ({Status}).", code, spec.Status);
            }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "seed";

            var lower = value.Trim().ToLowerInvariant();
            var normalised = lower.Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder(normalised.Length);
            foreach (var ch in normalised)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);

                if (cat != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-]", " ");
            cleaned = Regex.Replace(cleaned, @"[\s-]+", "-").Trim('-');

            if (cleaned.Length > 180)
                cleaned = cleaned[..180].Trim('-');

            return string.IsNullOrEmpty(cleaned) ? "seed" : cleaned;
        }

        private static string UniqueSlug(string baseSlug)
        {
            return Slugify(baseSlug);
        }
    }
}
