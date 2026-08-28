using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ZansiHustle.Application.Orders.Dtos;
using ZansiHustle.Shared.Errors;

namespace ZansiHustle.Tests.Orders
{
    public class VariantAwareOrderTests
    {
        [Fact]
        public async Task VariantProduct_RequiresVariantId()
        {
            using var h = new VariantOrderTestHarness();
            var listing = h.AddListing("iPhone 17 Pro Max", 25999m, null,
                VariantOrderTestHarness.Variant("128GB · Black", false, null, 4));

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, Quantity = 1 } }, // no VariantId
            });

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Fact]
        public async Task NoVariantListing_StillWorks_UnchangedBehaviour()
        {
            using var h = new VariantOrderTestHarness();
            var listing = h.AddListing("Plain T-Shirt", 199m, stock: 10);

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, Quantity = 2 } },
            });

            Assert.True(result.IsSuccess);
            var item = result.Data!.Items.Single();
            Assert.Equal(199m, item.UnitPrice);
            Assert.Equal(398m, item.LineTotal);
        }

        [Fact]
        public async Task VariantMustBelongToListing()
        {
            using var h = new VariantOrderTestHarness();
            var listingA = h.AddListing("Product A", 100m, null, VariantOrderTestHarness.Variant("A1", false, null, 5));
            var listingB = h.AddListing("Product B", 200m, null, VariantOrderTestHarness.Variant("B1", false, null, 5));

            var foreignVariantId = listingB.Variants.Single().Id;

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listingA.Id, VariantId = foreignVariantId, Quantity = 1 } },
            });

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Fact]
        public async Task InactiveVariant_Rejected()
        {
            using var h = new VariantOrderTestHarness();
            var inactive = VariantOrderTestHarness.Variant("Discontinued", false, null, 5, isActive: false);
            var listing = h.AddListing("Product", 100m, null, inactive);

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, VariantId = inactive.Id, Quantity = 1 } },
            });

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Fact]
        public async Task VariantPrice_IsUsed_BrowserCannotOverridePrice()
        {
            using var h = new VariantOrderTestHarness();
            var customPriced = VariantOrderTestHarness.Variant("256GB · Black", usesCustomPrice: true, price: 28999m, stock: 3);
            var listing = h.AddListing("iPhone 17 Pro Max", 25999m, null, customPriced);

            // CreateOrderItemDto has no price field at all — there is no
            // mechanism to send one, which is the actual guarantee. This
            // asserts the server-computed price matches the variant, never
            // anything a client could have supplied.
            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, VariantId = customPriced.Id, Quantity = 1 } },
            });

            Assert.True(result.IsSuccess);
            var item = result.Data!.Items.Single();
            Assert.Equal(28999m, item.UnitPrice); // variant's own price, NOT listing.Price (25999)
        }

        [Fact]
        public async Task BaseVariant_InheritsListingPrice()
        {
            using var h = new VariantOrderTestHarness();
            var baseVariant = VariantOrderTestHarness.Variant("128GB · Black", usesCustomPrice: false, price: null, stock: 4);
            var listing = h.AddListing("iPhone 17 Pro Max", 25999m, null, baseVariant);

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, VariantId = baseVariant.Id, Quantity = 1 } },
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(25999m, result.Data!.Items.Single().UnitPrice);
        }

        [Fact]
        public async Task OrderItem_SnapshotsVariantIdNameAndSku()
        {
            using var h = new VariantOrderTestHarness();
            var variant = VariantOrderTestHarness.Variant("256GB · Black", true, 28999m, 3);
            var listing = h.AddListing("iPhone 17 Pro Max", 25999m, null, variant);

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, VariantId = variant.Id, Quantity = 1 } },
            });

            Assert.True(result.IsSuccess);
            var item = result.Data!.Items.Single();
            Assert.Equal(variant.Id, item.VariantId);
            Assert.Equal("256GB · Black", item.VariantName);
            Assert.Equal(variant.Sku, item.Sku);
        }

        [Fact]
        public async Task HistoricalOrder_SurvivesLaterCatalogUpdate()
        {
            using var h = new VariantOrderTestHarness();
            var variant = VariantOrderTestHarness.Variant("256GB · Black", true, 28999m, 3);
            var listing = h.AddListing("iPhone 17 Pro Max", 25999m, null, variant);

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, VariantId = variant.Id, Quantity = 1 } },
            });
            Assert.True(result.IsSuccess);
            var orderId = result.Data!.Id;

            // Catalog changes AFTER the order was placed: price + name both change.
            var trackedVariant = await h.DbContext.ListingVariants.FindAsync(variant.Id);
            trackedVariant!.Name = "256GB · Midnight (Renamed)";
            trackedVariant.Price = 30999m;
            await h.DbContext.SaveChangesAsync();
            h.DbContext.ChangeTracker.Clear();

            var reloadedOrder = await h.OrderRepository.GetByIdAsync(orderId);
            var historicalItem = reloadedOrder!.Items.Single();

            Assert.Equal(28999m, historicalItem.UnitPrice); // price at purchase time, unchanged
            Assert.Equal("256GB · Black", historicalItem.VariantNameSnapshot); // name at purchase time, unchanged
        }

        [Fact]
        public async Task VariantOutOfStock_Rejected()
        {
            using var h = new VariantOrderTestHarness();
            var soldOut = VariantOrderTestHarness.Variant("Sold Out Colour", false, null, stock: 0);
            var listing = h.AddListing("Product", 100m, null, soldOut);

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, VariantId = soldOut.Id, Quantity = 1 } },
            });

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Fact]
        public async Task VariantQuantityExceedsStock_Rejected()
        {
            using var h = new VariantOrderTestHarness();
            var limited = VariantOrderTestHarness.Variant("Limited Colour", false, null, stock: 2);
            var listing = h.AddListing("Product", 100m, null, limited);

            var result = await h.Service.CreateAsync(h.BuyerUserId, new CreateOrderRequestDto
            {
                Items = { new CreateOrderItemDto { ListingId = listing.Id, VariantId = limited.Id, Quantity = 5 } },
            });

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }
    }
}
