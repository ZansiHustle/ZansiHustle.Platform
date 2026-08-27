using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Payments.External;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Infrastructure.Payments.External;
using ZansiHustle.Infrastructure.Payments.Ozow;
using ZansiHustle.Infrastructure.Persistence.Payments;

namespace ZansiHustle.Tests.Payments.External
{
    /// <summary>
    /// Wires a fully-real ExternalShopPaymentService against an EF Core
    /// InMemory AppDbContext (real repositories, real hash/signature crypto)
    /// with only the network-facing edges faked (IOzowClient, callback HTTP
    /// sender) — per the "no live Ozow call required" test requirement.
    /// </summary>
    public sealed class TestHarness : IDisposable
    {
        public const string TestShopCode = "zansitech";
        public const string TestSharedSecret = "unit-test-shared-secret-value";
        public const string TestReturnHost = "uatapi.zansitech.com";

        public AppDbContext DbContext { get; }
        public FakeOzowClient OzowClient { get; } = new();
        public FakeExternalShopCallbackSender CallbackSender { get; } = new();
        public OzowSettings OzowSettings { get; } = new()
        {
            SiteCode = "TESTSITE",
            PrivateKey = "test-private-key-0123456789",
            ApiKey = "test-api-key",
            NotifyUrl = "https://uatapi.zansihustle.com/external-payments/ozow/webhook",
            IsTest = true,
        };

        public ExternalShopsOptions Shops { get; } = new()
        {
            [TestShopCode] = new ExternalShopOptions
            {
                Enabled = true,
                ShopName = "ZansiTech",
                SharedSecret = TestSharedSecret,
                AllowedReturnHosts = { TestReturnHost },
                AllowedCallbackHosts = { TestReturnHost },
            },
        };

        public ExternalPaymentsSettings Settings { get; } = new()
        {
            ApiBaseUrl = "https://uatapi.zansihustle.com",
            SessionExpiryHours = 24,
            CallbackTimeoutSeconds = 15,
        };

        private readonly IOzowHashService _ozowHashService;
        public ExternalShopPaymentService Service { get; }
        public PaymentRepository PaymentRepository { get; }
        public ExternalPaymentSessionRepository SessionRepository { get; }

        public TestHarness()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            DbContext = new AppDbContext(options);

            _ozowHashService = new OzowHashService(Options.Create(OzowSettings), NullLogger<OzowHashService>.Instance);
            PaymentRepository = new PaymentRepository(DbContext);
            SessionRepository = new ExternalPaymentSessionRepository(DbContext);

            Service = new ExternalShopPaymentService(
                SessionRepository,
                PaymentRepository,
                OzowClient,
                _ozowHashService,
                new ExternalShopSignatureService(),
                CallbackSender,
                Options.Create(Shops),
                Options.Create(Settings),
                NullLogger<ExternalShopPaymentService>.Instance);
        }

        /// <summary>
        /// Computes a valid inbound notification Hash using the EXACT same
        /// field order/format as <c>OzowHashService.ValidateNotificationHash</c>
        /// so tests can construct realistic, verifiable webhook payloads
        /// without a live Ozow call.
        /// </summary>
        public string ComputeValidNotificationHash(OzowTransactionNotification n)
        {
            var sb = new StringBuilder(512);
            sb.Append(n.SiteCode ?? string.Empty);
            sb.Append(n.TransactionId ?? string.Empty);
            sb.Append(n.TransactionReference ?? string.Empty);
            sb.Append(n.Amount.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(n.Status ?? string.Empty);
            sb.Append(n.Optional1 ?? string.Empty);
            sb.Append(n.Optional2 ?? string.Empty);
            sb.Append(n.Optional3 ?? string.Empty);
            sb.Append(n.Optional4 ?? string.Empty);
            sb.Append(n.Optional5 ?? string.Empty);
            sb.Append(n.CurrencyCode ?? string.Empty);
            sb.Append(n.IsTest ? "true" : "false");
            sb.Append(n.StatusMessage ?? string.Empty);
            sb.Append(OzowSettings.PrivateKey);

            var lowered = sb.ToString().ToLowerInvariant();
            var bytes = Encoding.UTF8.GetBytes(lowered);
            var digest = SHA512.HashData(bytes);
            return Convert.ToHexString(digest).ToLowerInvariant();
        }

        public void Dispose() => DbContext.Dispose();
    }
}
