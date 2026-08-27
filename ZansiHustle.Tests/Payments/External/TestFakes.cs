using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Payments.External;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Tests.Payments.External
{
    /// <summary>
    /// In-memory stand-in for IOzowClient. No live Ozow call is ever made in
    /// tests — CreatePaymentRequestAsync and GetTransactionByReferenceAsync
    /// return pre-programmed results the test configures.
    /// </summary>
    public sealed class FakeOzowClient : IOzowClient
    {
        public bool IsConfigured { get; set; } = true;
        public bool UatTestMode { get; set; } = false;
        public decimal UatTestAmount { get; set; } = 10m;

        public Result<OzowPaymentRequestResult> NextCreateResult { get; set; } =
            Result<OzowPaymentRequestResult>.Success(new OzowPaymentRequestResult { Url = "https://pay.ozow.com/abc123", PaymentRequestId = "req-1" });

        public Result<OzowTransactionModel> NextLookupResult { get; set; } =
            Result<OzowTransactionModel>.Failure(ErrorCodes.NotFound, "no lookup configured");

        public int CreatePaymentRequestCallCount { get; private set; }
        public OzowPaymentRequest? LastRequest { get; private set; }

        public IReadOnlyList<string> GetMissingFieldEnvVars() => new List<string>();

        public Task<Result<OzowTokenResponse>> GetTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<OzowTokenResponse>.Success(new OzowTokenResponse { AccessToken = "test" }));

        public Task<Result<OzowPaymentRequestResult>> CreatePaymentRequestAsync(OzowPaymentRequest request, CancellationToken cancellationToken = default)
        {
            CreatePaymentRequestCallCount++;
            LastRequest = request;
            return Task.FromResult(NextCreateResult);
        }

        public Task<Result<OzowTransactionModel>> GetTransactionByReferenceAsync(string transactionReference, CancellationToken cancellationToken = default) =>
            Task.FromResult(NextLookupResult);

        public Task<Result<OzowTransactionModel>> GetTransactionAsync(string transactionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(NextLookupResult);
    }

    /// <summary>
    /// Records every callback delivery attempt so tests can assert on the
    /// exact bytes signed/sent, and can force delivery failure to prove a
    /// merchant-side outage never undoes a settled payment.
    /// </summary>
    public sealed class FakeExternalShopCallbackSender : IExternalShopCallbackSender
    {
        public bool ShouldSucceed { get; set; } = true;
        public string? FailureError { get; set; } = "simulated outage";

        public List<(string Url, byte[] Body, string SignatureHex)> Deliveries { get; } = new();

        public Task<(bool Success, string? Error)> SendAsync(string callbackUrl, byte[] bodyBytes, string signatureHex, CancellationToken cancellationToken = default)
        {
            Deliveries.Add((callbackUrl, bodyBytes, signatureHex));
            return Task.FromResult(ShouldSucceed ? (true, (string?)null) : (false, FailureError));
        }
    }
}
