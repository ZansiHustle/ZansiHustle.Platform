using System;
using System.Threading.Tasks;
using Xunit;
using ZansiHustle.Application.Payments.External;
using ZansiHustle.Application.Payments.External.Dtos;
using ZansiHustle.Shared.Errors;

namespace ZansiHustle.Tests.Payments.External
{
    public class StatusTests
    {
        private static CreateExternalPaymentSessionRequestDto ValidRequest(string shopCode, string returnHost) => new()
        {
            ShopCode = shopCode,
            ShopName = "Test Shop",
            ExternalOrderId = "order-status-1",
            ExternalOrderNumber = "ORD-1",
            Amount = 100m,
            Currency = "ZAR",
            ReturnUrl = $"https://{returnHost}/return",
            CallbackUrl = $"https://{returnHost}/callback",
        };

        [Fact]
        public async Task StatusEndpoint_RequiresSharedSecret()
        {
            using var h = new TestHarness();
            var create = await h.Service.CreateSessionAsync(ValidRequest(TestHarness.TestShopCode, TestHarness.TestReturnHost), TestHarness.TestSharedSecret);

            var status = await h.Service.GetStatusAsync(create.Data!.SessionId, providedSharedSecret: null);

            Assert.False(status.IsSuccess);
            Assert.Equal(ErrorCodes.Unauthorized, status.Code);
        }

        [Fact]
        public async Task StatusEndpoint_UnknownSession_ReturnsNotFound()
        {
            using var h = new TestHarness();

            var status = await h.Service.GetStatusAsync(Guid.NewGuid(), TestHarness.TestSharedSecret);

            Assert.False(status.IsSuccess);
            Assert.Equal(ErrorCodes.NotFound, status.Code);
        }

        [Fact]
        public async Task StatusEndpoint_OwnershipProtected_DifferentShopsCannotAccessEachOthersSessions()
        {
            using var h = new TestHarness();
            h.Shops["otherbrand"] = new ExternalShopOptions
            {
                Enabled = true,
                ShopName = "Other Brand",
                SharedSecret = "other-secret",
                AllowedReturnHosts = { "uat.otherbrand.example" },
                AllowedCallbackHosts = { "uat.otherbrand.example" },
            };

            var create = await h.Service.CreateSessionAsync(ValidRequest(TestHarness.TestShopCode, TestHarness.TestReturnHost), TestHarness.TestSharedSecret);
            Assert.True(create.IsSuccess);

            // "otherbrand" presents ITS OWN valid secret, but tries to read zansitech's session.
            var status = await h.Service.GetStatusAsync(create.Data!.SessionId, "other-secret");

            Assert.False(status.IsSuccess);
            Assert.Equal(ErrorCodes.NotFound, status.Code); // masked as not-found, never confirms existence
        }

        [Fact]
        public async Task StatusEndpoint_OwningShop_SeesAuthoritativeStatus()
        {
            using var h = new TestHarness();
            var create = await h.Service.CreateSessionAsync(ValidRequest(TestHarness.TestShopCode, TestHarness.TestReturnHost), TestHarness.TestSharedSecret);

            var status = await h.Service.GetStatusAsync(create.Data!.SessionId, TestHarness.TestSharedSecret);

            Assert.True(status.IsSuccess);
            Assert.Equal("RedirectCreated", status.Data!.Status);
        }
    }
}
