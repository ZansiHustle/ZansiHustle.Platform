using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ZansiHustle.Application.Notifications;
using ZansiHustle.Application.Orders;
using ZansiHustle.Application.Persistence.ServiceBookings;
using ZansiHustle.Application.Wallets;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Infrastructure.Persistence.Listings;
using ZansiHustle.Infrastructure.Persistence.Orders;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Enums.Merchants;

namespace ZansiHustle.Tests.Orders
{
    /// <summary>
    /// Wires a fully-real OrderService against an EF Core InMemory
    /// AppDbContext (real OrderRepository/ListingRepository — the two
    /// dependencies this test suite actually exercises) with the remaining
    /// constructor dependencies mocked, since CreateAsync never calls them
    /// unless the request supplies a DeliveryQuoteOptionId or ServiceBooking
    /// (neither of which these product-order tests set).
    /// </summary>
    public sealed class VariantOrderTestHarness : IDisposable
    {
        public AppDbContext DbContext { get; }
        public Merchant Merchant { get; }
        public Guid BuyerUserId { get; } = Guid.NewGuid();

        public OrderRepository OrderRepository { get; }
        public ListingRepository ListingRepository { get; }
        public OrderService Service { get; }

        public VariantOrderTestHarness()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            DbContext = new AppDbContext(options);

            Merchant = new Merchant
            {
                Id = Guid.NewGuid(),
                Code = "MERCH-ORDTEST",
                Slug = "order-test-merchant",
                Name = "Order Test Merchant",
                Type = MerchantType.OnlineStore,
                Status = MerchantStatus.Active,
                OwnerUserId = Guid.NewGuid(), // distinct from BuyerUserId — buyer never owns their own shop
                CreatedAtUtc = DateTime.UtcNow,
            };
            DbContext.Merchants.Add(Merchant);
            DbContext.SaveChanges();

            OrderRepository = new OrderRepository(DbContext);
            ListingRepository = new ListingRepository(DbContext);

            var buyer = new User { Id = BuyerUserId, FirstName = "Test", LastName = "Buyer", Email = "buyer@test.com", UserName = "buyer@test.com" };
            var userManagerMock = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            userManagerMock.Setup(m => m.FindByIdAsync(BuyerUserId.ToString())).ReturnsAsync(buyer);

            Service = new OrderService(
                OrderRepository,
                ListingRepository,
                Mock.Of<IServiceBookingRepository>(),
                userManagerMock.Object,
                Mock.Of<IZansiDispatchService>(),
                Mock.Of<IWalletService>(),
                Mock.Of<INotificationService>(),
                NullLogger<OrderService>.Instance);
        }

        /// <summary>Adds a Listing with the given variants (or none) owned by the harness's Merchant.</summary>
        public Listing AddListing(string title, decimal price, int? stock = null, params ListingVariant[] variants)
        {
            var listing = new Listing
            {
                Id = Guid.NewGuid(),
                Code = $"LIS-{Guid.NewGuid():N}".Substring(0, 20),
                Slug = $"listing-{Guid.NewGuid():N}".Substring(0, 20),
                Type = ListingType.Product,
                Status = ListingStatus.Active,
                ListingSource = ListingSource.SellerAccount,
                MerchantId = Merchant.Id,
                Title = title,
                Price = price,
                Stock = stock,
                CreatedAtUtc = DateTime.UtcNow,
            };
            foreach (var v in variants)
            {
                v.ListingId = listing.Id;
                listing.Variants.Add(v);
            }
            DbContext.Listings.Add(listing);
            DbContext.SaveChanges();
            DbContext.ChangeTracker.Clear();
            return listing;
        }

        public static ListingVariant Variant(string name, bool usesCustomPrice, decimal? price, int? stock, bool isActive = true) => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            UsesCustomPrice = usesCustomPrice,
            Price = usesCustomPrice ? price : null,
            Stock = stock,
            Sku = $"SKU-{name}",
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
        };

        public void Dispose() => DbContext.Dispose();
    }
}
