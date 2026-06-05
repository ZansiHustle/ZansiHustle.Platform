using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Payments.Ozow
{
    /// <summary>
    /// HTTP implementation of <see cref="IOzowClient"/>.
    /// Typed via <see cref="IHttpClientFactory"/>; the factory injects the
    /// base URL, the static <c>ApiKey</c> header, and a 30-second timeout.
    /// Hash generation is delegated — this class never touches PrivateKey.
    /// </summary>
    public sealed class OzowClient : IOzowClient
    {
        private readonly HttpClient _http;
        private readonly OzowSettings _settings;
        private readonly IOzowHashService _hashService;
        private readonly ILogger<OzowClient> _logger;

        public OzowClient(
            HttpClient http,
            IOptions<OzowSettings> settings,
            IOzowHashService hashService,
            ILogger<OzowClient> logger)
        {
            _http = http;
            _settings = settings.Value ?? new OzowSettings();
            _hashService = hashService;
            _logger = logger;
        }

        public bool IsConfigured => _settings.HasCredentials() && _hashService.IsImplemented;

        public bool UatTestMode => _settings.UatTestMode;

        public decimal UatTestAmount => _settings.UatTestAmount > 0m ? _settings.UatTestAmount : 10m;

        public async Task<Result<OzowTokenResponse>> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            if (!_settings.HasCredentials())
                return Result<OzowTokenResponse>.Failure(ErrorCodes.ProviderNotConfigured, "Ozow credentials are not configured.");

            try
            {
                var form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("siteCode", _settings.SiteCode),
                    new KeyValuePair<string, string>("apiKey", _settings.ApiKey),
                    new KeyValuePair<string, string>("grantType", "client_credentials"),
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, "/token") { Content = form };
                using var response = await _http.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Ozow token request failed with {StatusCode}: {Body}", response.StatusCode, text);
                    return Result<OzowTokenResponse>.Failure(ErrorCodes.Exception, $"Ozow token error ({(int)response.StatusCode}).");
                }

                var data = await response.Content.ReadFromJsonAsync<OzowTokenResponse>(cancellationToken: cancellationToken);

                if (data is null || string.IsNullOrWhiteSpace(data.AccessToken))
                    return Result<OzowTokenResponse>.Failure(ErrorCodes.Exception, "Ozow token response was empty.");

                return Result<OzowTokenResponse>.Success(data, "Ozow token retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ozow token request threw.");
                return Result<OzowTokenResponse>.Failure(ErrorCodes.Exception, $"Ozow token request failed. {ex.Message}");
            }
        }

        public async Task<Result<OzowPaymentRequestResult>> CreatePaymentRequestAsync(
            OzowPaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.HasCredentials())
                return Result<OzowPaymentRequestResult>.Failure(ErrorCodes.ProviderNotConfigured, "Ozow credentials are not configured.");

            if (!_hashService.IsImplemented)
            {
                _logger.LogError(
                    "Refusing to call Ozow PostPaymentRequest because IOzowHashService is not yet implemented. " +
                    "See OzowHashService.cs TODO. TransactionReference={Ref}",
                    request.TransactionReference);
                return Result<OzowPaymentRequestResult>.Failure(
                    ErrorCodes.ProviderNotConfigured,
                    "Ozow hash service is not implemented. The PostPaymentRequest call would be rejected by Ozow with a hash mismatch.");
            }

            try
            {
                request.SiteCode = _settings.SiteCode;
                request.IsTest = _settings.IsTest;
                request.CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? _settings.CountryCode : request.CountryCode;
                request.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? _settings.CurrencyCode : request.CurrencyCode;
                request.SuccessUrl = string.IsNullOrWhiteSpace(request.SuccessUrl) ? _settings.SuccessUrl : request.SuccessUrl;
                request.CancelUrl = string.IsNullOrWhiteSpace(request.CancelUrl) ? _settings.CancelUrl : request.CancelUrl;
                request.ErrorUrl = string.IsNullOrWhiteSpace(request.ErrorUrl) ? _settings.ErrorUrl : request.ErrorUrl;
                request.NotifyUrl = string.IsNullOrWhiteSpace(request.NotifyUrl) ? _settings.NotifyUrl : request.NotifyUrl;

                // Normalise the decimal scale to 2 places so the JSON wire
                // body and the hash input agree on the exact string form.
                //   • Math.Round(5m, 2) → 5m       (scale 0, JSON emits "5")
                //   • Math.Round(5m, 2) + 0.00m → 5.00m (scale 2, JSON emits "5.00")
                // OzowHashService formats the amount as "0.00" invariant for
                // the SHA512 input regardless, but pinning the scale here
                // keeps the wire body unambiguous and matches what other
                // Ozow integrations send. The numeric value is unchanged.
                request.Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero) + 0.00m;

                request.HashCheck = _hashService.GenerateRequestHash(request);

                // Pre-call log. Never log ApiKey/PrivateKey/HashCheck — only
                // the safe call-context fields. UAT debugging happens against
                // the deployed API, so logs need to be informative enough to
                // diagnose remotely without leaking credentials.
                _logger.LogInformation(
                    "[Ozow] PostPaymentRequest → ref={Ref} amount={Amount} {Currency} site={Site} isTest={IsTest} bankRef={BankRef}",
                    request.TransactionReference, request.Amount, request.CurrencyCode,
                    request.SiteCode, request.IsTest, request.BankReference);

                using var response = await _http.PostAsJsonAsync("/PostPaymentRequest", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "[Ozow] PostPaymentRequest FAILED status={StatusCode} ref={Ref} body={Body}",
                        (int)response.StatusCode, request.TransactionReference, text);
                    return Result<OzowPaymentRequestResult>.Failure(ErrorCodes.Exception, $"Ozow error ({(int)response.StatusCode}).");
                }

                var data = await response.Content.ReadFromJsonAsync<OzowPaymentRequestResult>(cancellationToken: cancellationToken);

                if (data is null || string.IsNullOrWhiteSpace(data.Url) || string.IsNullOrWhiteSpace(data.PaymentRequestId))
                {
                    _logger.LogWarning(
                        "[Ozow] PostPaymentRequest returned non-actionable payload ref={Ref} error={Error}",
                        request.TransactionReference, data?.ErrorMessage);
                    return Result<OzowPaymentRequestResult>.Failure(ErrorCodes.Exception, data?.ErrorMessage ?? "Ozow returned an empty response.");
                }

                _logger.LogInformation(
                    "[Ozow] PostPaymentRequest OK ref={Ref} paymentRequestId={Prid}",
                    request.TransactionReference, data.PaymentRequestId);

                return Result<OzowPaymentRequestResult>.Success(data, "Ozow PostPaymentRequest succeeded.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ozow PostPaymentRequest threw for ref {Ref}.", request.TransactionReference);
                return Result<OzowPaymentRequestResult>.Failure(ErrorCodes.Exception, $"Ozow PostPaymentRequest failed. {ex.Message}");
            }
        }

        public async Task<Result<OzowTransactionModel>> GetTransactionByReferenceAsync(
            string transactionReference,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.HasCredentials())
                return Result<OzowTransactionModel>.Failure(ErrorCodes.ProviderNotConfigured, "Ozow credentials are not configured.");

            if (string.IsNullOrWhiteSpace(transactionReference))
                return Result<OzowTransactionModel>.Failure(ErrorCodes.BadRequest, "TransactionReference is required.");

            try
            {
                var path = $"/GetTransactionByReference?siteCode={Uri.EscapeDataString(_settings.SiteCode)}&transactionReference={Uri.EscapeDataString(transactionReference)}&IsTest={(_settings.IsTest ? "true" : "false")}";
                return await GetTransactionInternalAsync(path, transactionReference, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ozow GetTransactionByReference threw for ref {Ref}.", transactionReference);
                return Result<OzowTransactionModel>.Failure(ErrorCodes.Exception, $"Ozow lookup failed. {ex.Message}");
            }
        }

        public async Task<Result<OzowTransactionModel>> GetTransactionAsync(
            string transactionId,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.HasCredentials())
                return Result<OzowTransactionModel>.Failure(ErrorCodes.ProviderNotConfigured, "Ozow credentials are not configured.");

            if (string.IsNullOrWhiteSpace(transactionId))
                return Result<OzowTransactionModel>.Failure(ErrorCodes.BadRequest, "TransactionId is required.");

            try
            {
                var path = $"/GetTransaction?siteCode={Uri.EscapeDataString(_settings.SiteCode)}&transactionId={Uri.EscapeDataString(transactionId)}&IsTest={(_settings.IsTest ? "true" : "false")}";
                return await GetTransactionInternalAsync(path, transactionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ozow GetTransaction threw for txId {TxId}.", transactionId);
                return Result<OzowTransactionModel>.Failure(ErrorCodes.Exception, $"Ozow lookup failed. {ex.Message}");
            }
        }

        private async Task<Result<OzowTransactionModel>> GetTransactionInternalAsync(
            string path,
            string contextRef,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("[Ozow] Lookup → ref={Ref}", contextRef);

            using var response = await _http.GetAsync(path, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var text = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "[Ozow] Lookup FAILED status={StatusCode} ref={Ref} body={Body}",
                    (int)response.StatusCode, contextRef, text);
                return Result<OzowTransactionModel>.Failure(ErrorCodes.Exception, $"Ozow error ({(int)response.StatusCode}).");
            }

            // Ozow returns either a single object or an array depending on the endpoint —
            // GetTransactionByReference returns an array, GetTransaction returns a single
            // object. Try array first; fall back to single object.
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(raw))
                return Result<OzowTransactionModel>.Failure(ErrorCodes.Exception, "Ozow returned an empty body.");

            try
            {
                OzowTransactionModel? model;
                if (raw.TrimStart().StartsWith('['))
                {
                    var arr = System.Text.Json.JsonSerializer.Deserialize<OzowTransactionModel[]>(raw);
                    model = arr?.FirstOrDefault();

                    if (model is null)
                        return Result<OzowTransactionModel>.Failure(ErrorCodes.NotFound, "Ozow returned no transactions for this reference.");
                }
                else
                {
                    model = System.Text.Json.JsonSerializer.Deserialize<OzowTransactionModel>(raw);

                    if (model is null)
                        return Result<OzowTransactionModel>.Failure(ErrorCodes.Exception, "Ozow returned an unparseable body.");
                }

                _logger.LogInformation(
                    "[Ozow] Lookup OK ref={Ref} status={Status} amount={Amount} {Currency}",
                    contextRef, model.Status, model.Amount, model.CurrencyCode);

                return Result<OzowTransactionModel>.Success(model, "Ozow lookup succeeded.");
            }
            catch (System.Text.Json.JsonException jex)
            {
                _logger.LogError(jex, "Ozow lookup payload was not valid JSON for {Ref}.", contextRef);
                return Result<OzowTransactionModel>.Failure(ErrorCodes.Exception, "Ozow returned malformed JSON.");
            }
        }

        /// <summary>
        /// Registered by DI as the typed <see cref="HttpClient"/> configurator.
        /// Centralises base URL + ApiKey header so callers never handle credentials.
        /// </summary>
        public static void ConfigureHttpClient(HttpClient client, OzowSettings settings)
        {
            var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
                ? "https://stagingapi.ozow.com"
                : settings.BaseUrl.TrimEnd('/');

            client.BaseAddress = new Uri(baseUrl + "/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                // Ozow authenticates server-to-server calls with a static ApiKey header,
                // not a Bearer token. The token endpoint is only needed for a small
                // subset of merchant-management calls.
                client.DefaultRequestHeaders.Add("ApiKey", settings.ApiKey);
            }

            client.Timeout = TimeSpan.FromSeconds(30);
        }
    }
}
