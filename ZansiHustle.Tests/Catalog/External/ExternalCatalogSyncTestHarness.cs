using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Catalog.External;
using ZansiHustle.Domain.Merchants;
using ZansiHustle.Domain.Shops;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Infrastructure.Persistence.ExternalCatalog;
using ZansiHustle.Infrastructure.Persistence.Listings;
using ZansiHustle.Infrastructure.Persistence.Shops;
using ZansiHustle.Shared.Enums.Merchants;
using ZansiHustle.Shared.Enums.Shops;

namespace ZansiHustle.Tests.Catalog.External
{
    /// <summary>
    /// Wires a fully-real ExternalCatalogSyncService against an EF Core
    /// InMemory AppDbContext (real ListingRepository/ShopProfileRepository/
    /// ExternalCatalogRepository) with a pre-seeded Merchant + ShopProfile
    /// standing in for the registered source's configured shop.
    /// </summary>
    public sealed class ExternalCatalogSyncTestHarness : IDisposable
    {
        public const string TestSourceCode = "zansitech";
        public const string TestSharedSecret = "unit-test-catalog-secret";

        public AppDbContext DbContext { get; }
        public Merchant Merchant { get; }
        public ShopProfile ShopProfile { get; }

        public ExternalCatalogSourcesOptions Sources { get; } = new();

        public ListingRepository ListingRepository { get; }
        public ExternalCatalogRepository CatalogRepository { get; }
        public ExternalCatalogSyncService Service { get; }

        public ExternalCatalogSyncTestHarness()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            DbContext = new AppDbContext(options);

            Merchant = new Merchant
            {
                Id = Guid.NewGuid(),
                Code = "MERCH-TEST",
                Slug = "test-merchant",
                Name = "Test Merchant",
                Type = MerchantType.OnlineStore,
                Status = MerchantStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            };

            ShopProfile = new ShopProfile
            {
                Id = Guid.NewGuid(),
                MerchantId = Merchant.Id,
                Slug = "test-shop",
                Name = "Test Shop",
                Status = ShopProfileStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
            };

            DbContext.Merchants.Add(Merchant);
            DbContext.ShopProfiles.Add(ShopProfile);
            DbContext.SaveChanges();

            Sources[TestSourceCode] = new ExternalCatalogSourceOptions
            {
                Enabled = true,
                SourceName = "ZansiTech",
                ShopProfileId = ShopProfile.Id,
                SharedSecret = TestSharedSecret,
            };

            ListingRepository = new ListingRepository(DbContext);
            var shopProfileRepository = new ShopProfileRepository(DbContext);
            CatalogRepository = new ExternalCatalogRepository(DbContext);

            Service = new ExternalCatalogSyncService(
                ListingRepository,
                shopProfileRepository,
                CatalogRepository,
                Options.Create(Sources),
                NullLogger<ExternalCatalogSyncService>.Instance);
        }

        public void Dispose() => DbContext.Dispose();
    }
}
