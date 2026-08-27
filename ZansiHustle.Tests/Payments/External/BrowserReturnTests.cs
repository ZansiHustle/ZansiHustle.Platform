using System.Threading.Tasks;
using Xunit;
using ZansiHustle.Application.Payments.External.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Shared.Enums.Payments;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Tests.Payments.External
{
    public class BrowserReturnTests
    {
        private static CreateExternalPaymentSessionRequestDto ValidRequest() => new()
        {
            ShopCode = TestHarness.TestShopCode,
            ShopName = "ZansiTech",
            ExternalOrderId = "order-return-1",
            ExternalOrderNumber = "ORD-RET-1",
            Amount = 300m,
            Currency = "ZAR",
            ReturnUrl = $"https://{TestHarness.TestReturnHost}/api/payments/zansihustle/return",
            CallbackUrl = $"https://{TestHarness.TestReturnHost}/api/payments/zansihustle/callback",
        };

        [Fact]
        public async Task UnknownReference_ReturnsNull()
        {
            using var h = new TestHarness();

            var redirect = await h.Service.ResolveBrowserReturnAsync("no-such-reference");

            Assert.Null(redirect);
        }

        [Fact]
        public async Task BrowserReturn_RedirectsOnlyToConfiguredShopReturnUrl_WithSessionQueryParam()
        {
            using var h = new TestHarness();
            var create = await h.Service.CreateSessionAsync(ValidRequest(), TestHarness.TestSharedSecret);
            var session = await h.SessionRepository.GetByIdAsync(create.Data!.SessionId);

            // Ozow's own lookup is inconclusive (still Pending) — return must still resolve to the shop's URL.
            h.OzowClient.NextLookupResult = Result<OzowTransactionModel>.Success(new OzowTransactionModel
            {
                Status = "Pending",
                Amount = session!.Amount,
                CurrencyCode = session.Currency,
            });

            var redirect = await h.Service.ResolveBrowserReturnAsync(session.ProviderReference);

            Assert.NotNull(redirect);
            Assert.StartsWith(session.ReturnUrl, redirect!.ToString());
            Assert.Contains($"session={session.Id}", redirect.ToString());
        }

        [Fact]
        public async Task BrowserReturn_DoesNotSettlePaymentFromUnverifiedQueryString()
        {
            using var h = new TestHarness();
            var create = await h.Service.CreateSessionAsync(ValidRequest(), TestHarness.TestSharedSecret);
            var session = await h.SessionRepository.GetByIdAsync(create.Data!.SessionId);

            // Ozow's own API says the transaction is still pending — the browser
            // return must NEVER independently decide "success" from a query string.
            h.OzowClient.NextLookupResult = Result<OzowTransactionModel>.Success(new OzowTransactionModel
            {
                Status = "Pending",
                Amount = session!.Amount,
                CurrencyCode = session.Currency,
            });

            await h.Service.ResolveBrowserReturnAsync(session.ProviderReference);

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.NotEqual(ExternalPaymentSessionStatus.Paid, reloaded!.Status);
        }

        [Fact]
        public async Task BrowserReturn_CanReconcileToPaidViaOzowsOwnVerifiedLookup()
        {
            using var h = new TestHarness();
            var create = await h.Service.CreateSessionAsync(ValidRequest(), TestHarness.TestSharedSecret);
            var session = await h.SessionRepository.GetByIdAsync(create.Data!.SessionId);

            // This is Ozow's OWN API response (not the browser's query string) —
            // legitimate best-effort reconciliation, per spec section 16.
            h.OzowClient.NextLookupResult = Result<OzowTransactionModel>.Success(new OzowTransactionModel
            {
                Status = "Complete",
                Amount = session!.Amount,
                CurrencyCode = session.Currency,
            });

            await h.Service.ResolveBrowserReturnAsync(session.ProviderReference);

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.Equal(ExternalPaymentSessionStatus.Paid, reloaded!.Status);
        }
    }
}
