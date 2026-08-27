using System.Threading.Tasks;
using Xunit;
using ZansiHustle.Application.Payments.External.Dtos;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Domain.Payments.External;
using ZansiHustle.Shared.Enums.Payments;

namespace ZansiHustle.Tests.Payments.External
{
    public class WebhookTests
    {
        private static CreateExternalPaymentSessionRequestDto ValidRequest(string externalOrderId) => new()
        {
            ShopCode = TestHarness.TestShopCode,
            ShopName = "ZansiTech",
            ExternalOrderId = externalOrderId,
            ExternalOrderNumber = "ZT-2001",
            Amount = 250.00m,
            Currency = "ZAR",
            ReturnUrl = $"https://{TestHarness.TestReturnHost}/api/payments/zansihustle/return",
            CallbackUrl = $"https://{TestHarness.TestReturnHost}/api/payments/zansihustle/callback",
        };

        private async Task<ExternalPaymentSession> CreateSessionAsync(TestHarness h, string externalOrderId)
        {
            var result = await h.Service.CreateSessionAsync(ValidRequest(externalOrderId), TestHarness.TestSharedSecret);
            Assert.True(result.IsSuccess);
            return (await h.SessionRepository.GetByIdAsync(result.Data!.SessionId))!;
        }

        private OzowTransactionNotification BuildNotification(TestHarness h, ExternalPaymentSession session, string status, decimal? amount = null, string? currency = null, string transactionId = "OZW-TX-1")
        {
            var n = new OzowTransactionNotification
            {
                SiteCode = h.OzowSettings.SiteCode,
                TransactionId = transactionId,
                TransactionReference = session.ProviderReference,
                Amount = amount ?? session.Amount,
                Status = status,
                Optional1 = session.Id.ToString(),
                Optional2 = session.ShopCode,
                Optional3 = session.ExternalOrderId,
                CurrencyCode = currency ?? session.Currency,
                IsTest = true,
                StatusMessage = status,
            };
            n.Hash = h.ComputeValidNotificationHash(n);
            return n;
        }

        [Fact]
        public async Task VerifiedOzowPaid_UpdatesExternalSession()
        {
            using var h = new TestHarness();
            var session = await CreateSessionAsync(h, "order-paid-1");

            var notification = BuildNotification(h, session, "Complete");
            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw");

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.Equal(ExternalPaymentSessionStatus.Paid, reloaded!.Status);
            Assert.NotNull(reloaded.PaidAtUtc);
        }

        [Fact]
        public async Task InvalidHash_DoesNotSettle()
        {
            using var h = new TestHarness();
            var session = await CreateSessionAsync(h, "order-badhash-1");

            var notification = BuildNotification(h, session, "Complete");
            notification.Hash = "not-a-real-hash";

            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw");

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.Equal(ExternalPaymentSessionStatus.RedirectCreated, reloaded!.Status);
        }

        [Fact]
        public async Task DuplicatePaidNotification_IsIdempotent()
        {
            using var h = new TestHarness();
            var session = await CreateSessionAsync(h, "order-dup-1");
            var notification = BuildNotification(h, session, "Complete");

            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw");
            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw"); // exact duplicate

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.Equal(ExternalPaymentSessionStatus.Paid, reloaded!.Status);

            // Callback must only have been attempted once, not once per duplicate webhook.
            Assert.Single(h.CallbackSender.Deliveries);
        }

        [Fact]
        public async Task OutOfOrderFailureCannotDowngradePaid()
        {
            using var h = new TestHarness();
            var session = await CreateSessionAsync(h, "order-outoforder-1");

            var paid = BuildNotification(h, session, "Complete", transactionId: "OZW-TX-1");
            await h.Service.HandleOzowWebhookAsync(paid, rawBody: "raw");

            var laterCancel = BuildNotification(h, session, "Cancelled", transactionId: "OZW-TX-2");
            await h.Service.HandleOzowWebhookAsync(laterCancel, rawBody: "raw");

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.Equal(ExternalPaymentSessionStatus.Paid, reloaded!.Status);
        }

        [Fact]
        public async Task AmountMismatch_RejectedDoesNotSettle()
        {
            using var h = new TestHarness();
            var session = await CreateSessionAsync(h, "order-amtmismatch-1");

            var notification = BuildNotification(h, session, "Complete", amount: session.Amount + 100m);
            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw");

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.Equal(ExternalPaymentSessionStatus.Failed, reloaded!.Status);
            Assert.Contains("mismatch", reloaded.FailureReason, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CurrencyMismatch_RejectedDoesNotSettle()
        {
            using var h = new TestHarness();
            var session = await CreateSessionAsync(h, "order-ccymismatch-1");

            var notification = BuildNotification(h, session, "Complete", currency: "USD");
            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw");

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.Equal(ExternalPaymentSessionStatus.Failed, reloaded!.Status);
        }

        [Fact]
        public async Task PaidSendsSignedCallback_HmacMatchesZansiTechContract()
        {
            using var h = new TestHarness();
            var session = await CreateSessionAsync(h, "order-callback-1");

            var notification = BuildNotification(h, session, "Complete");
            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw");

            Assert.Single(h.CallbackSender.Deliveries);
            var (url, body, signatureHex) = h.CallbackSender.Deliveries[0];

            Assert.Equal(session.CallbackUrl, url);

            // Independent recomputation using the raw contract: HMAC-SHA256(secret, rawBodyBytes) -> lowercase hex.
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(TestHarness.TestSharedSecret));
            var expected = System.Convert.ToHexString(hmac.ComputeHash(body)).ToLowerInvariant();
            Assert.Equal(expected, signatureHex);

            // The body itself must be the frozen ZansiTech shape.
            var json = System.Text.Encoding.UTF8.GetString(body);
            Assert.Contains("\"shopCode\":\"zansitech\"", json);
            Assert.Contains("\"status\":\"Paid\"", json);
        }

        [Fact]
        public async Task FailedCallbackDeliveryDoesNotUndoPayment()
        {
            using var h = new TestHarness();
            h.CallbackSender.ShouldSucceed = false;
            var session = await CreateSessionAsync(h, "order-callbackfail-1");

            var notification = BuildNotification(h, session, "Complete");
            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw");

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.Equal(ExternalPaymentSessionStatus.Paid, reloaded!.Status); // still Paid
            Assert.Null(reloaded.CallbackDeliveredAtUtc);
            Assert.NotNull(reloaded.LastCallbackError);
        }

        [Fact]
        public async Task StatusPoll_RetriesUndeliveredCallback()
        {
            using var h = new TestHarness();
            h.CallbackSender.ShouldSucceed = false;
            var session = await CreateSessionAsync(h, "order-retry-1");

            var notification = BuildNotification(h, session, "Complete");
            await h.Service.HandleOzowWebhookAsync(notification, rawBody: "raw");
            Assert.Single(h.CallbackSender.Deliveries); // first attempt failed

            h.CallbackSender.ShouldSucceed = true;
            var status = await h.Service.GetStatusAsync(session.Id, TestHarness.TestSharedSecret);

            Assert.True(status.IsSuccess);
            Assert.Equal("Paid", status.Data!.Status);
            Assert.Equal(2, h.CallbackSender.Deliveries.Count); // opportunistic retry on read

            var reloaded = await h.SessionRepository.GetByIdAsync(session.Id);
            Assert.NotNull(reloaded!.CallbackDeliveredAtUtc);
        }
    }
}
