using System;
using System.Threading.Tasks;
using Xunit;
using ZansiHustle.Application.Payments.External.Dtos;
using ZansiHustle.Shared.Errors;

namespace ZansiHustle.Tests.Payments.External
{
    public class CreateSessionTests
    {
        private static CreateExternalPaymentSessionRequestDto ValidRequest(string externalOrderId = "order-1") => new()
        {
            ShopCode = TestHarness.TestShopCode,
            ShopName = "ZansiTech",
            ExternalOrderId = externalOrderId,
            ExternalOrderNumber = "ZT-1001",
            Amount = 499.99m,
            Currency = "ZAR",
            CustomerEmail = "buyer@example.com",
            ReturnUrl = $"https://{TestHarness.TestReturnHost}/api/payments/zansihustle/return",
            CallbackUrl = $"https://{TestHarness.TestReturnHost}/api/payments/zansihustle/callback",
        };

        [Fact]
        public async Task RegisteredShop_CanCreateSession_ReturnsSessionIdAndRedirectUrl()
        {
            using var h = new TestHarness();

            var result = await h.Service.CreateSessionAsync(ValidRequest(), TestHarness.TestSharedSecret);

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Data!.SessionId);
            Assert.False(string.IsNullOrWhiteSpace(result.Data!.RedirectUrl));
            Assert.Equal(1, h.OzowClient.CreatePaymentRequestCallCount);

            // Ozow must be told to call ZansiHustle back — never the shop directly.
            Assert.Equal("https://uatapi.zansihustle.com/external-payments/ozow/webhook", h.OzowClient.LastRequest!.NotifyUrl);
            Assert.Equal("https://uatapi.zansihustle.com/external-payments/ozow/return/success", h.OzowClient.LastRequest!.SuccessUrl);
        }

        [Fact]
        public async Task UnknownShop_IsRejected()
        {
            using var h = new TestHarness();
            var request = ValidRequest();
            request.ShopCode = "not-a-real-shop";

            var result = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.Unauthorized, result.Code);
            Assert.Equal(0, h.OzowClient.CreatePaymentRequestCallCount);
        }

        [Fact]
        public async Task WrongSharedSecret_IsRejected()
        {
            using var h = new TestHarness();

            var result = await h.Service.CreateSessionAsync(ValidRequest(), "totally-wrong-secret");

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.Unauthorized, result.Code);
            Assert.Equal(0, h.OzowClient.CreatePaymentRequestCallCount);
        }

        [Fact]
        public async Task MissingSharedSecret_IsRejected()
        {
            using var h = new TestHarness();

            var result = await h.Service.CreateSessionAsync(ValidRequest(), providedSharedSecret: null);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.Unauthorized, result.Code);
        }

        [Fact]
        public async Task DisabledShop_IsRejected()
        {
            using var h = new TestHarness();
            h.Shops[TestHarness.TestShopCode].Enabled = false;

            var result = await h.Service.CreateSessionAsync(ValidRequest(), TestHarness.TestSharedSecret);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.Forbidden, result.Code);
        }

        [Fact]
        public async Task InvalidReturnHost_IsRejected()
        {
            using var h = new TestHarness();
            var request = ValidRequest();
            request.ReturnUrl = "https://evil-attacker.example.com/return";

            var result = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
            Assert.Equal(0, h.OzowClient.CreatePaymentRequestCallCount);
        }

        [Fact]
        public async Task InvalidCallbackHost_IsRejected()
        {
            using var h = new TestHarness();
            var request = ValidRequest();
            request.CallbackUrl = "https://evil-attacker.example.com/callback";

            var result = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Fact]
        public async Task NonHttpsCallbackUrl_IsRejected()
        {
            using var h = new TestHarness();
            var request = ValidRequest();
            request.CallbackUrl = $"http://{TestHarness.TestReturnHost}/callback"; // http, not https

            var result = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Fact]
        public async Task LocalhostCallbackUrl_IsRejected()
        {
            using var h = new TestHarness();
            var request = ValidRequest();
            request.CallbackUrl = "https://localhost/callback";

            var result = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public async Task AmountLessOrEqualZero_IsRejected(decimal amount)
        {
            using var h = new TestHarness();
            var request = ValidRequest();
            request.Amount = amount;

            var result = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Fact]
        public async Task UnsupportedCurrency_IsRejected()
        {
            using var h = new TestHarness();
            var request = ValidRequest();
            request.Currency = "USD";

            var result = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCodes.BadRequest, result.Code);
        }

        [Fact]
        public async Task SameShopSameExternalOrder_IsIdempotent()
        {
            using var h = new TestHarness();
            var request = ValidRequest("order-idempotent-1");

            var first = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);
            var second = await h.Service.CreateSessionAsync(request, TestHarness.TestSharedSecret);

            Assert.True(first.IsSuccess);
            Assert.True(second.IsSuccess);
            Assert.Equal(first.Data!.SessionId, second.Data!.SessionId);
            Assert.Equal(first.Data!.RedirectUrl, second.Data!.RedirectUrl);

            // The critical assertion: retries must never spawn a second Ozow payment.
            Assert.Equal(1, h.OzowClient.CreatePaymentRequestCallCount);
        }

        [Fact]
        public async Task DifferentShopsWithSameExternalOrderId_GetIndependentSessions()
        {
            using var h = new TestHarness();
            h.Shops["otherbrand"] = new ZansiHustle.Application.Payments.External.ExternalShopOptions
            {
                Enabled = true,
                ShopName = "Other Brand",
                SharedSecret = "other-secret",
                AllowedReturnHosts = { "uat.otherbrand.example" },
                AllowedCallbackHosts = { "uat.otherbrand.example" },
            };

            var forZansiTech = ValidRequest("shared-order-id");
            var forOtherBrand = new CreateExternalPaymentSessionRequestDto
            {
                ShopCode = "otherbrand",
                ShopName = "Other Brand",
                ExternalOrderId = "shared-order-id", // same external order id, different shop
                ExternalOrderNumber = "OB-1",
                Amount = 10m,
                Currency = "ZAR",
                ReturnUrl = "https://uat.otherbrand.example/return",
                CallbackUrl = "https://uat.otherbrand.example/callback",
            };

            var r1 = await h.Service.CreateSessionAsync(forZansiTech, TestHarness.TestSharedSecret);
            var r2 = await h.Service.CreateSessionAsync(forOtherBrand, "other-secret");

            Assert.True(r1.IsSuccess);
            Assert.True(r2.IsSuccess);
            Assert.NotEqual(r1.Data!.SessionId, r2.Data!.SessionId);
            Assert.Equal(2, h.OzowClient.CreatePaymentRequestCallCount);
        }
    }
}
