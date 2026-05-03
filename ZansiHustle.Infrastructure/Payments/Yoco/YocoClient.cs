using System;
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

namespace ZansiHustle.Infrastructure.Payments.Yoco
{
    /// <summary>
    /// HTTP implementation of <see cref="IYocoClient"/>.
    /// Typed via <see cref="IHttpClientFactory"/>; the factory injects the
    /// base URL, the Bearer-token Authorization header, and a 30-second
    /// timeout. SecretKey never leaves this class — request/response logs
    /// only reference the Yoco checkout id and our own metadata.
    /// </summary>
    public sealed class YocoClient : IYocoClient
    {
        private readonly HttpClient _http;
        private readonly YocoSettings _settings;
        private readonly IYocoSignatureService _signatureService;
        private readonly ILogger<YocoClient> _logger;

        public YocoClient(
            HttpClient http,
            IOptions<YocoSettings> settings,
            IYocoSignatureService signatureService,
            ILogger<YocoClient> logger)
        {
            _http = http;
            _settings = settings.Value ?? new YocoSettings();
            _signatureService = signatureService;
            _logger = logger;
        }

        public bool IsConfigured => _settings.HasCredentials() && _signatureService.IsImplemented;

        public bool UatTestMode => _settings.UatTestMode;

        public decimal UatTestAmount => _settings.UatTestAmount > 0m ? _settings.UatTestAmount : 10m;

        public async Task<Result<YocoCheckoutResponse>> CreateCheckoutAsync(
            YocoCheckoutRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.HasCredentials())
                return Result<YocoCheckoutResponse>.Failure(ErrorCodes.ProviderNotConfigured, "Yoco credentials are not configured.");

            try
            {
                // Pre-call log. Never log SecretKey or full metadata. The
                // Yoco checkout id is unknown until response — log on return.
                _logger.LogInformation(
                    "[Yoco] CreateCheckout → amount={Amount} {Currency} hasMetadata={HasMeta}",
                    request.Amount, request.Currency, request.Metadata is { Count: > 0 });

                using var response = await _http.PostAsJsonAsync("/checkouts", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "[Yoco] CreateCheckout FAILED status={StatusCode} body={Body}",
                        (int)response.StatusCode, text);
                    return Result<YocoCheckoutResponse>.Failure(ErrorCodes.Exception, $"Yoco error ({(int)response.StatusCode}).");
                }

                var data = await response.Content.ReadFromJsonAsync<YocoCheckoutResponse>(cancellationToken: cancellationToken);

                if (data is null || string.IsNullOrWhiteSpace(data.Id) || string.IsNullOrWhiteSpace(data.RedirectUrl))
                {
                    _logger.LogWarning("[Yoco] CreateCheckout returned non-actionable payload.");
                    return Result<YocoCheckoutResponse>.Failure(ErrorCodes.Exception, "Yoco returned an empty checkout response.");
                }

                _logger.LogInformation(
                    "[Yoco] CreateCheckout OK id={Id} status={Status} paymentStatus={PaymentStatus}",
                    data.Id, data.Status, data.PaymentStatus);

                return Result<YocoCheckoutResponse>.Success(data, "Yoco checkout created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Yoco] CreateCheckout threw.");
                return Result<YocoCheckoutResponse>.Failure(ErrorCodes.Exception, $"Yoco CreateCheckout failed. {ex.Message}");
            }
        }

        public async Task<Result<YocoCheckoutResponse>> GetCheckoutAsync(
            string checkoutId,
            CancellationToken cancellationToken = default)
        {
            if (!_settings.HasCredentials())
                return Result<YocoCheckoutResponse>.Failure(ErrorCodes.ProviderNotConfigured, "Yoco credentials are not configured.");

            if (string.IsNullOrWhiteSpace(checkoutId))
                return Result<YocoCheckoutResponse>.Failure(ErrorCodes.BadRequest, "Yoco checkout id is required.");

            try
            {
                _logger.LogInformation("[Yoco] GetCheckout → id={Id}", checkoutId);

                using var response = await _http.GetAsync($"/checkouts/{Uri.EscapeDataString(checkoutId)}", cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return Result<YocoCheckoutResponse>.Failure(ErrorCodes.NotFound, "Yoco checkout not found.");

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "[Yoco] GetCheckout FAILED status={StatusCode} id={Id} body={Body}",
                        (int)response.StatusCode, checkoutId, text);
                    return Result<YocoCheckoutResponse>.Failure(ErrorCodes.Exception, $"Yoco error ({(int)response.StatusCode}).");
                }

                var data = await response.Content.ReadFromJsonAsync<YocoCheckoutResponse>(cancellationToken: cancellationToken);

                if (data is null)
                    return Result<YocoCheckoutResponse>.Failure(ErrorCodes.Exception, "Yoco returned an empty body.");

                _logger.LogInformation(
                    "[Yoco] GetCheckout OK id={Id} status={Status} paymentStatus={PaymentStatus}",
                    data.Id, data.Status, data.PaymentStatus);

                return Result<YocoCheckoutResponse>.Success(data, "Yoco checkout retrieved.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Yoco] GetCheckout threw for id {Id}.", checkoutId);
                return Result<YocoCheckoutResponse>.Failure(ErrorCodes.Exception, $"Yoco GetCheckout failed. {ex.Message}");
            }
        }

        /// <summary>
        /// Registered by DI as the typed <see cref="HttpClient"/> configurator.
        /// Centralises base URL + Bearer auth so callers never handle credentials.
        /// </summary>
        public static void ConfigureHttpClient(HttpClient client, YocoSettings settings)
        {
            var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
                ? "https://payments.yoco.com/api"
                : settings.BaseUrl.TrimEnd('/');

            client.BaseAddress = new Uri(baseUrl + "/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(settings.SecretKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.SecretKey);
            }

            client.Timeout = TimeSpan.FromSeconds(30);
        }
    }
}
