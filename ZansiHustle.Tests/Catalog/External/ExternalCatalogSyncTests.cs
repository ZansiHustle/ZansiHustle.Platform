using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using ZansiHustle.Application.Catalog.External;
using ZansiHustle.Application.Catalog.External.Dtos;
using ZansiHustle.Domain.Listings;
using ZansiHustle.Shared.Enums.Listings;
using ZansiHustle.Shared.Errors;

namespace ZansiHustle.Tests.Catalog.External
{
    public class ExternalCatalogSyncTests
    {
        private static ExternalProductUpsertRequestDto BuildProduct(
            string externalProductId,
            string title = "iPhone 17 Pro Max",
            decimal basePrice = 25999.00m,
            DateTime? sourceUpdatedAtUtc = null,
            params ExternalProductVariantDto[] variants)
        {
            return new ExternalProductUpsertRequestDto
            {
                ExternalProductId = externalProductId,
                Title = title,
                Description = "Flagship phone.",
                BasePrice = basePrice,
                Currency = "ZAR",
                Status = "Active",
                Images = new List<string> { "https://media.zansitech.com/iphone17.jpg" },
                SourceUpdatedAtUtc = sourceUpdatedAtUtc ?? DateTime.UtcNow,
                Variants = variants.Length > 0
                    ? variants.ToList()
                    : new List<ExternalProductVariantDto>
                    {
                        new() { ExternalVariantId = "var-128-black", Sku = "IP17-128-BLK", Name = "128GB · Black", Price = basePrice, IsActive = true, AvailableQuantity = 4 },
                    },
            };
        }

        [Fact]
        public async Task ValidRegisteredSource_UpsertsProduct()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var result = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-1"), syncRunId: null);

            Assert.True(result.IsSuccess);
            Assert.Equal("Created", result.Data!.Result);
            Assert.NotEqual(Guid.Empty, result.Data.ListingId);

            var listing = await h.ListingRepository.GetByIdAsync(result.Data.ListingId);
            Assert.NotNull(listing);
            Assert.Equal(h.ShopProfile.Id, listing!.ShopProfileId);
            Assert.Equal(h.Merchant.Id, listing.MerchantId);
            Assert.Equal(ListingSource.ShopProfile, listing.ListingSource);
            Assert.Equal("zansitech", listing.ExternalSourceCode);
        }

        [Fact]
        public async Task WrongSecret_Rejected()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var result = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, "wrong-secret", BuildProduct("prod-2"), null);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.Unauthorized, result.Code);
        }

        [Fact]
        public async Task UnknownSource_Rejected()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var result = await h.Service.UpsertProductAsync(
                "not-a-real-source", ExternalCatalogSyncTestHarness.TestSharedSecret, BuildProduct("prod-3"), null);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.Unauthorized, result.Code);
        }

        [Fact]
        public async Task DisabledSource_Rejected()
        {
            using var h = new ExternalCatalogSyncTestHarness();
            h.Sources[ExternalCatalogSyncTestHarness.TestSourceCode].Enabled = false;

            var result = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, BuildProduct("prod-4"), null);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.Forbidden, result.Code);
        }

        [Fact]
        public async Task SameExternalProductId_Twice_SameListingId()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var first = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-5", sourceUpdatedAtUtc: DateTime.UtcNow), null);

            var second = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-5", sourceUpdatedAtUtc: DateTime.UtcNow.AddMinutes(1)), null);

            Assert.True(first.IsSuccess);
            Assert.True(second.IsSuccess);
            Assert.Equal(first.Data!.ListingId, second.Data!.ListingId);
            Assert.Equal("Updated", second.Data.Result);
        }

        [Fact]
        public async Task UpdatedTitle_SameListingId_NewTitle()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var first = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-6", title: "iPhone 17", sourceUpdatedAtUtc: DateTime.UtcNow), null);

            var second = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-6", title: "iPhone 17 Pro Max (Updated)", sourceUpdatedAtUtc: DateTime.UtcNow.AddMinutes(1)), null);

            Assert.Equal(first.Data!.ListingId, second.Data!.ListingId);
            var listing = await h.ListingRepository.GetByIdAsync(second.Data.ListingId);
            Assert.Equal("iPhone 17 Pro Max (Updated)", listing!.Title);
        }

        [Fact]
        public async Task SameExternalVariantId_SameListingVariantId_NewExternalVariantId_NewVariant()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var v1 = new ExternalProductVariantDto { ExternalVariantId = "var-A", Sku = "SKU-A", Name = "128GB", Price = 25999m, IsActive = true, AvailableQuantity = 5 };
            var first = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-7", basePrice: 25999m, sourceUpdatedAtUtc: DateTime.UtcNow, variants: v1), null);

            var firstVariantId = first.Data!.VariantIds["var-A"];

            // Second sync: existing variant updated (same externalVariantId), plus one brand new variant.
            var v1Updated = new ExternalProductVariantDto { ExternalVariantId = "var-A", Sku = "SKU-A", Name = "128GB", Price = 25999m, IsActive = true, AvailableQuantity = 2 };
            var v2New = new ExternalProductVariantDto { ExternalVariantId = "var-B", Sku = "SKU-B", Name = "256GB", Price = 28999m, IsActive = true, AvailableQuantity = 7 };
            var second = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-7", basePrice: 25999m, sourceUpdatedAtUtc: DateTime.UtcNow.AddMinutes(1), variants: new[] { v1Updated, v2New }), null);

            Assert.True(second.IsSuccess, $"{second.Code}: {second.Message}");
            Assert.Equal(firstVariantId, second.Data!.VariantIds["var-A"]); // stable id
            Assert.NotEqual(Guid.Empty, second.Data.VariantIds["var-B"]); // new row

            var listing = await h.ListingRepository.GetByIdAsync(first.Data.ListingId);
            Assert.Equal(2, listing!.Variants.Count(v => v.IsActive));
            var updatedVariant = listing.Variants.First(v => v.Id == firstVariantId);
            Assert.Equal(2, updatedVariant.Stock);
        }

        [Fact]
        public async Task RemovedExternalVariant_DeactivatedSafely_NeverDeleted()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var v1 = new ExternalProductVariantDto { ExternalVariantId = "var-A", Name = "128GB", Price = 100m, IsActive = true, AvailableQuantity = 1 };
            var v2 = new ExternalProductVariantDto { ExternalVariantId = "var-B", Name = "256GB", Price = 100m, IsActive = true, AvailableQuantity = 1 };
            var first = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-8", basePrice: 100m, sourceUpdatedAtUtc: DateTime.UtcNow, variants: new[] { v1, v2 }), null);

            var variantBId = first.Data!.VariantIds["var-B"];

            // Second sync omits var-B entirely — it must be deactivated, not gone.
            var second = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-8", basePrice: 100m, sourceUpdatedAtUtc: DateTime.UtcNow.AddMinutes(1), variants: v1), null);

            Assert.True(second.IsSuccess);
            var listing = await h.ListingRepository.GetByIdAsync(first.Data.ListingId);
            var removedVariant = listing!.Variants.FirstOrDefault(v => v.Id == variantBId);
            Assert.NotNull(removedVariant); // still exists — never deleted
            Assert.False(removedVariant!.IsActive); // but deactivated
        }

        [Fact]
        public async Task SourceA_CannotOverwrite_SourceB()
        {
            using var h = new ExternalCatalogSyncTestHarness();
            h.Sources["otherbrand"] = new ExternalCatalogSourceOptions
            {
                Enabled = true,
                SourceName = "Other Brand",
                ShopProfileId = h.ShopProfile.Id, // same shop, different source code — still must not collide
                SharedSecret = "other-secret",
            };

            var a = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("shared-id", title: "ZansiTech Product"), null);

            var b = await h.Service.UpsertProductAsync(
                "otherbrand", "other-secret",
                BuildProduct("shared-id", title: "Other Brand Product"), null);

            Assert.True(a.IsSuccess);
            Assert.True(b.IsSuccess);
            Assert.NotEqual(a.Data!.ListingId, b.Data!.ListingId); // independent rows despite the same externalProductId string

            var listingA = await h.ListingRepository.GetByIdAsync(a.Data.ListingId);
            Assert.Equal("ZansiTech Product", listingA!.Title); // untouched by source B's write
        }

        [Fact]
        public async Task ManualNonExternalListing_NeverTouchedByArchiveBoundary()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var manualListing = new Listing
            {
                Id = Guid.NewGuid(),
                Code = "LIS-MANUAL-1",
                Slug = "manual-listing",
                Type = ListingType.Product,
                Status = ListingStatus.Active,
                ListingSource = ListingSource.SellerAccount,
                MerchantId = h.Merchant.Id,
                Title = "Manually created listing",
                Price = 50m,
                ExternalSourceCode = null, // never touched by sync
                CreatedAtUtc = DateTime.UtcNow,
            };
            h.DbContext.Listings.Add(manualListing);
            h.DbContext.SaveChanges();

            var runStart = await h.Service.StartFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret);
            await h.Service.CompleteFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, runStart.Data!.SyncRunId);

            var reloaded = await h.ListingRepository.GetByIdAsync(manualListing.Id);
            Assert.Equal(ListingStatus.Active, reloaded!.Status); // still active — never archived
        }

        [Fact]
        public async Task PriceMapping_BaseAndCustomPrice_MappedCorrectly()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var baseVariant = new ExternalProductVariantDto { ExternalVariantId = "var-base", Name = "128GB", Price = 25999m, IsActive = true, AvailableQuantity = 1 };
            var customVariant = new ExternalProductVariantDto { ExternalVariantId = "var-custom", Name = "256GB", Price = 28999m, IsActive = true, AvailableQuantity = 1 };

            var result = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-price", basePrice: 25999m, variants: new[] { baseVariant, customVariant }), null);

            var listing = await h.ListingRepository.GetByIdAsync(result.Data!.ListingId);
            Assert.Equal(25999m, listing!.Price);

            var baseRow = listing.Variants.First(v => v.ExternalVariantId == "var-base");
            Assert.False(baseRow.UsesCustomPrice);
            Assert.Null(baseRow.Price);

            var customRow = listing.Variants.First(v => v.ExternalVariantId == "var-custom");
            Assert.True(customRow.UsesCustomPrice);
            Assert.Equal(28999m, customRow.Price);
        }

        [Fact]
        public async Task AvailableQuantity_MappedToStockProjection_ImagesMapped()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var v1 = new ExternalProductVariantDto { ExternalVariantId = "var-1", Name = "A", Price = 10m, IsActive = true, AvailableQuantity = 4 };
            var v2 = new ExternalProductVariantDto { ExternalVariantId = "var-2", Name = "B", Price = 10m, IsActive = true, AvailableQuantity = 2 };

            var result = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-stock", basePrice: 10m, variants: new[] { v1, v2 }), null);

            var listing = await h.ListingRepository.GetByIdAsync(result.Data!.ListingId);
            Assert.Equal(6, listing!.Stock); // sum of variant availableQuantity
            Assert.Equal(4, listing.Variants.First(v => v.ExternalVariantId == "var-1").Stock);
            Assert.Single(listing.Images);
            Assert.Equal("https://media.zansitech.com/iphone17.jpg", listing.Images[0]);
        }

        [Fact]
        public async Task OldSourceUpdatedAtUtc_Ignored()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var newer = DateTime.UtcNow;
            var first = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-stale", title: "Newer Title", sourceUpdatedAtUtc: newer), null);

            var older = newer.AddHours(-1);
            var stale = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-stale", title: "Stale Older Title", sourceUpdatedAtUtc: older), null);

            Assert.True(stale.IsSuccess);
            Assert.Equal("SkippedStale", stale.Data!.Result);

            var listing = await h.ListingRepository.GetByIdAsync(first.Data!.ListingId);
            Assert.Equal("Newer Title", listing!.Title); // the stale write never applied
        }

        [Fact]
        public async Task ArchiveProduct_Works()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var created = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("prod-archive"), null);

            var archived = await h.Service.ArchiveProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, "prod-archive");

            Assert.True(archived.IsSuccess);

            var listing = await h.ListingRepository.GetByIdAsync(created.Data!.ListingId);
            Assert.Equal(ListingStatus.Archived, listing!.Status);
            Assert.All(listing.Variants, v => Assert.False(v.IsActive));
        }

        [Fact]
        public async Task FullSnapshot_CreatesAllProducts_SecondRunIsIdempotent()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var start = await h.Service.StartFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret);
            Assert.True(start.IsSuccess);

            var batchRequest = new ExternalCatalogBatchUpsertRequestDto
            {
                Products = new List<ExternalProductUpsertRequestDto>
                {
                    BuildProduct("snap-1"),
                    BuildProduct("snap-2"),
                },
            };
            var batch = await h.Service.UpsertBatchAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start.Data!.SyncRunId, batchRequest);

            Assert.True(batch.IsSuccess);
            Assert.Equal(2, batch.Data!.Succeeded);

            var complete = await h.Service.CompleteFullSyncRunAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start.Data.SyncRunId);
            Assert.True(complete.IsSuccess);
            Assert.Equal(0, complete.Data!.ProductsArchived); // nothing pre-existed to archive

            // Second full run with the SAME two products — idempotent re-run.
            var start2 = await h.Service.StartFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret);
            var batch2 = await h.Service.UpsertBatchAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start2.Data!.SyncRunId,
                new ExternalCatalogBatchUpsertRequestDto { Products = new List<ExternalProductUpsertRequestDto> { BuildProduct("snap-1"), BuildProduct("snap-2") } });
            var complete2 = await h.Service.CompleteFullSyncRunAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start2.Data.SyncRunId);

            Assert.True(batch2.IsSuccess);
            Assert.Equal(0, complete2.Data!.ProductsArchived); // both products present again — nothing archived
        }

        [Fact]
        public async Task SnapshotMissingProduct_ArchivesOnlyAfterCompletion()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var start1 = await h.Service.StartFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret);
            await h.Service.UpsertBatchAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start1.Data!.SyncRunId,
                new ExternalCatalogBatchUpsertRequestDto { Products = new List<ExternalProductUpsertRequestDto> { BuildProduct("keep-1"), BuildProduct("drop-1") } });
            var complete1 = await h.Service.CompleteFullSyncRunAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start1.Data.SyncRunId);
            Assert.Equal(0, complete1.Data!.ProductsArchived);

            var keptListing = await h.ListingRepository.GetByExternalIdAsync(ExternalCatalogSyncTestHarness.TestSourceCode, "keep-1");
            var droppedListing = await h.ListingRepository.GetByExternalIdAsync(ExternalCatalogSyncTestHarness.TestSourceCode, "drop-1");
            Assert.NotNull(keptListing);
            Assert.NotNull(droppedListing);

            // Second full run's snapshot no longer includes "drop-1".
            var start2 = await h.Service.StartFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret);
            await h.Service.UpsertBatchAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start2.Data!.SyncRunId,
                new ExternalCatalogBatchUpsertRequestDto { Products = new List<ExternalProductUpsertRequestDto> { BuildProduct("keep-1") } });

            // Not yet completed — "drop-1" must still be Active.
            var stillActive = await h.ListingRepository.GetByExternalIdAsync(ExternalCatalogSyncTestHarness.TestSourceCode, "drop-1");
            Assert.Equal(ListingStatus.Active, stillActive!.Status);

            var complete2 = await h.Service.CompleteFullSyncRunAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start2.Data.SyncRunId);
            Assert.Equal(1, complete2.Data!.ProductsArchived);

            var nowArchived = await h.ListingRepository.GetByExternalIdAsync(ExternalCatalogSyncTestHarness.TestSourceCode, "drop-1");
            Assert.Equal(ListingStatus.Archived, nowArchived!.Status);

            var keptStillFine = await h.ListingRepository.GetByExternalIdAsync(ExternalCatalogSyncTestHarness.TestSourceCode, "keep-1");
            Assert.Equal(ListingStatus.Active, keptStillFine!.Status);
        }

        [Fact]
        public async Task PartialFailedSnapshot_DoesNotArchiveAbsentProducts()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var start1 = await h.Service.StartFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret);
            await h.Service.UpsertBatchAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start1.Data!.SyncRunId,
                new ExternalCatalogBatchUpsertRequestDto { Products = new List<ExternalProductUpsertRequestDto> { BuildProduct("persist-1") } });
            await h.Service.CompleteFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start1.Data.SyncRunId);

            // A second run starts, sends nothing (simulating a dropped connection),
            // then is explicitly marked FAILED — never completed.
            var start2 = await h.Service.StartFullSyncRunAsync(ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret);
            var failResult = await h.Service.FailFullSyncRunAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start2.Data!.SyncRunId, "network timeout");
            Assert.True(failResult.IsSuccess);

            // "persist-1" was never touched by the failed run and must remain Active —
            // a failed/abandoned run must NEVER be read as "delete everything missing".
            var listing = await h.ListingRepository.GetByExternalIdAsync(ExternalCatalogSyncTestHarness.TestSourceCode, "persist-1");
            Assert.Equal(ListingStatus.Active, listing!.Status);

            // Completing an already-failed run must be rejected, not silently archive.
            var completeAfterFail = await h.Service.CompleteFullSyncRunAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret, start2.Data.SyncRunId);
            Assert.False(completeAfterFail.IsSuccess);
            Assert.Equal(ErrorCodes.Conflict, completeAfterFail.Code);

            var stillActive = await h.ListingRepository.GetByExternalIdAsync(ExternalCatalogSyncTestHarness.TestSourceCode, "persist-1");
            Assert.Equal(ListingStatus.Active, stillActive!.Status);
        }

        // ─── Simple (variant-less) products ─────────────────────────────

        private static ExternalProductUpsertRequestDto BuildSimpleProduct(
            string externalProductId,
            string title = "USB-C Cable",
            decimal basePrice = 149.00m,
            int? availableQuantity = 20,
            DateTime? sourceUpdatedAtUtc = null)
        {
            return new ExternalProductUpsertRequestDto
            {
                ExternalProductId = externalProductId,
                Title = title,
                Description = "Simple single-SKU product, no variants.",
                BasePrice = basePrice,
                Currency = "ZAR",
                Status = "Active",
                Images = new List<string> { "https://media.zansitech.com/usb-c-cable.jpg" },
                SourceUpdatedAtUtc = sourceUpdatedAtUtc ?? DateTime.UtcNow,
                AvailableQuantity = availableQuantity,
                Variants = null, // omitted/null — simple product, per contract
            };
        }

        [Fact]
        public async Task CreateSimpleExternalProduct_NoVariantRows()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var result = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildSimpleProduct("simple-1", basePrice: 149m, availableQuantity: 20), syncRunId: null);

            Assert.True(result.IsSuccess, $"{result.Code}: {result.Message}");
            Assert.Equal("Created", result.Data!.Result);
            Assert.Empty(result.Data.VariantIds);

            var listing = await h.ListingRepository.GetByIdAsync(result.Data.ListingId);
            Assert.NotNull(listing);
            Assert.Empty(listing!.Variants);
            Assert.Equal(149m, listing.Price);
            Assert.Equal(20, listing.Stock);
            Assert.Equal("zansitech", listing.ExternalSourceCode);
        }

        [Fact]
        public async Task IdempotentSimpleProductUpdate_SameListingId_NewValuesApplied()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var first = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildSimpleProduct("simple-2", title: "USB-C Cable", basePrice: 149m, availableQuantity: 20, sourceUpdatedAtUtc: DateTime.UtcNow), null);

            var second = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildSimpleProduct("simple-2", title: "USB-C Cable (2m)", basePrice: 179m, availableQuantity: 5, sourceUpdatedAtUtc: DateTime.UtcNow.AddMinutes(1)), null);

            Assert.True(second.IsSuccess, $"{second.Code}: {second.Message}");
            Assert.Equal(first.Data!.ListingId, second.Data!.ListingId);
            Assert.Equal("Updated", second.Data.Result);

            var listing = await h.ListingRepository.GetByIdAsync(second.Data.ListingId);
            Assert.Empty(listing!.Variants);
            Assert.Equal("USB-C Cable (2m)", listing.Title);
            Assert.Equal(179m, listing.Price);
            Assert.Equal(5, listing.Stock);
        }

        [Fact]
        public async Task SimpleProduct_LaterGainsVariants_CreatesStableMappedVariants()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var simple = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildSimpleProduct("morphs-1", basePrice: 149m, availableQuantity: 20, sourceUpdatedAtUtc: DateTime.UtcNow), null);
            Assert.True(simple.IsSuccess, $"{simple.Code}: {simple.Message}");
            Assert.Empty(simple.Data!.VariantIds);

            var variant = new ExternalProductVariantDto { ExternalVariantId = "var-red", Name = "Red", Price = 149m, IsActive = true, AvailableQuantity = 8 };
            var becameVariant = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("morphs-1", basePrice: 149m, sourceUpdatedAtUtc: DateTime.UtcNow.AddMinutes(1), variants: variant), null);

            Assert.True(becameVariant.IsSuccess, $"{becameVariant.Code}: {becameVariant.Message}");
            Assert.Equal(simple.Data.ListingId, becameVariant.Data!.ListingId);
            Assert.Single(becameVariant.Data.VariantIds);

            var listing = await h.ListingRepository.GetByIdAsync(simple.Data.ListingId);
            var newVariant = Assert.Single(listing!.Variants);
            Assert.Equal("var-red", newVariant.ExternalVariantId);
            Assert.True(newVariant.IsActive);
            Assert.Equal(8, newVariant.Stock);
        }

        [Fact]
        public async Task VariantProduct_LaterBecomesSimple_SafelyDeactivatesPreviousVariants()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var v1 = new ExternalProductVariantDto { ExternalVariantId = "var-A", Name = "128GB", Price = 25999m, IsActive = true, AvailableQuantity = 4 };
            var v2 = new ExternalProductVariantDto { ExternalVariantId = "var-B", Name = "256GB", Price = 28999m, IsActive = true, AvailableQuantity = 3 };
            var variantProduct = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildProduct("morphs-2", basePrice: 25999m, sourceUpdatedAtUtc: DateTime.UtcNow, variants: new[] { v1, v2 }), null);
            Assert.True(variantProduct.IsSuccess, $"{variantProduct.Code}: {variantProduct.Message}");
            var variantAId = variantProduct.Data!.VariantIds["var-A"];
            var variantBId = variantProduct.Data.VariantIds["var-B"];

            var becameSimple = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildSimpleProduct("morphs-2", basePrice: 25999m, availableQuantity: 10, sourceUpdatedAtUtc: DateTime.UtcNow.AddMinutes(1)), null);

            Assert.True(becameSimple.IsSuccess, $"{becameSimple.Code}: {becameSimple.Message}");
            Assert.Equal(variantProduct.Data.ListingId, becameSimple.Data!.ListingId);
            Assert.Empty(becameSimple.Data.VariantIds);

            var listing = await h.ListingRepository.GetByIdAsync(variantProduct.Data.ListingId);
            Assert.Equal(25999m, listing!.Price);
            Assert.Equal(10, listing.Stock);

            // Previous variants still exist (never deleted) but are all deactivated.
            Assert.Equal(2, listing.Variants.Count);
            Assert.False(listing.Variants.First(v => v.Id == variantAId).IsActive);
            Assert.False(listing.Variants.First(v => v.Id == variantBId).IsActive);
        }

        [Fact]
        public async Task SimpleProduct_RemainsPurchasable_ThroughExistingVariantlessOrderPath()
        {
            using var h = new ExternalCatalogSyncTestHarness();

            var upserted = await h.Service.UpsertProductAsync(
                ExternalCatalogSyncTestHarness.TestSourceCode, ExternalCatalogSyncTestHarness.TestSharedSecret,
                BuildSimpleProduct("simple-order-1", basePrice: 149m, availableQuantity: 20), null);
            Assert.True(upserted.IsSuccess, $"{upserted.Code}: {upserted.Message}");

            var listing = await h.ListingRepository.GetByIdAsync(upserted.Data!.ListingId);
            Assert.Empty(listing!.Variants); // mirrored with zero ListingVariant rows, as designed

            // Exercise the ACTUAL, unmodified OrderService.CreateAsync path —
            // not just an assertion on the Listing's shape — to prove a
            // mirrored simple product is genuinely purchasable exactly like
            // any other variant-less listing.
            var buyerUserId = Guid.NewGuid();
            var buyer = new ZansiHustle.Domain.Identity.User
            {
                Id = buyerUserId,
                FirstName = "Test",
                LastName = "Buyer",
                Email = "buyer@test.com",
                UserName = "buyer@test.com",
            };
            var userManagerMock = new Moq.Mock<Microsoft.AspNetCore.Identity.UserManager<ZansiHustle.Domain.Identity.User>>(
                Moq.Mock.Of<Microsoft.AspNetCore.Identity.IUserStore<ZansiHustle.Domain.Identity.User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
            userManagerMock.Setup(m => m.FindByIdAsync(buyerUserId.ToString())).ReturnsAsync(buyer);

            var orderService = new ZansiHustle.Application.Orders.OrderService(
                new ZansiHustle.Infrastructure.Persistence.Orders.OrderRepository(h.DbContext),
                h.ListingRepository,
                Moq.Mock.Of<ZansiHustle.Application.Persistence.ServiceBookings.IServiceBookingRepository>(),
                userManagerMock.Object,
                Moq.Mock.Of<ZansiHustle.Application.ZansiDispatch.IZansiDispatchService>(),
                Moq.Mock.Of<ZansiHustle.Application.Wallets.IWalletService>(),
                Moq.Mock.Of<ZansiHustle.Application.Notifications.INotificationService>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ZansiHustle.Application.Orders.OrderService>.Instance);

            var orderResult = await orderService.CreateAsync(buyerUserId, new ZansiHustle.Application.Orders.Dtos.CreateOrderRequestDto
            {
                Items = { new ZansiHustle.Application.Orders.Dtos.CreateOrderItemDto { ListingId = listing.Id, Quantity = 2 } }, // no VariantId — variant-less listing
            });

            Assert.True(orderResult.IsSuccess, $"{orderResult.Code}: {orderResult.Message}");
            var item = orderResult.Data!.Items.Single();
            Assert.Null(item.VariantId);
            Assert.Equal(149m, item.UnitPrice);
            Assert.Equal(298m, item.LineTotal);
        }
    }
}
