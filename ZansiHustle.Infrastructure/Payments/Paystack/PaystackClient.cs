using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.Payments.Providers;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Payments.Paystack
{
    /// <summary>
    /// HTTP implementation of <see cref="IPaystackClient"/>.
    /// Typed via <see cref="IHttpClientFactory"/>; the factory injects base URL
    /// and bearer auth header on construction.
    /// </summary>
    public sealed class PaystackClient : IPaystackClient
    {
        private readonly HttpClient _http;
        private readonly PaystackSettings _settings;
        private readonly ILogger<PaystackClient> _logger;

        public PaystackClient(HttpClient http, IOptions<PaystackSettings> settings, ILogger<PaystackClient> logger)
        {
            _http = http;
            _settings = settings.Value ?? new PaystackSettings();
            _logger = logger;
        }

        public bool IsConfigured => _settings.HasCredentials();

        public string? PublicKey => string.IsNullOrWhiteSpace(_settings.PublicKey) ? null : _settings.PublicKey;

        public async Task<Result<PaystackInitializeResponse>> InitializeTransactionAsync(PaystackInitializeRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                return Result<PaystackInitializeResponse>.Failure(ErrorCodes.ProviderNotConfigured, "Paystack secret key is not configured.");

            try
            {
                using var response = await _http.PostAsJsonAsync("/transaction/initialize", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Paystack initialize failed with {StatusCode}: {Body}", response.StatusCode, text);
                    return Result<PaystackInitializeResponse>.Failure(ErrorCodes.Exception, $"Paystack error ({(int)response.StatusCode}).");
                }

                var data = await response.Content.ReadFromJsonAsync<PaystackInitializeResponse>(cancellationToken: cancellationToken);

                if (data is null || !data.Status || data.Data is null)
                {
                    _logger.LogWarning("Paystack initialize returned non-success payload: {Message}", data?.Message);
                    return Result<PaystackInitializeResponse>.Failure(ErrorCodes.Exception, data?.Message ?? "Paystack returned an empty response.");
                }

                return Result<PaystackInitializeResponse>.Success(data, "Paystack initialize succeeded.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack initialize threw.");
                return Result<PaystackInitializeResponse>.Failure(ErrorCodes.Exception, $"Paystack initialize failed. {ex.Message}");
            }
        }

        public async Task<Result<PaystackVerifyResponse>> VerifyTransactionAsync(string reference, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
                return Result<PaystackVerifyResponse>.Failure(ErrorCodes.ProviderNotConfigured, "Paystack secret key is not configured.");

            if (string.IsNullOrWhiteSpace(reference))
                return Result<PaystackVerifyResponse>.Failure(ErrorCodes.BadRequest, "Reference is required.");

            try
            {
                using var response = await _http.GetAsync($"/transaction/verify/{Uri.EscapeDataString(reference)}", cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Paystack verify failed with {StatusCode}: {Body}", response.StatusCode, text);
                    return Result<PaystackVerifyResponse>.Failure(ErrorCodes.Exception, $"Paystack error ({(int)response.StatusCode}).");
                }

                var data = await response.Content.ReadFromJsonAsync<PaystackVerifyResponse>(cancellationToken: cancellationToken);

                if (data is null)
                    return Result<PaystackVerifyResponse>.Failure(ErrorCodes.Exception, "Paystack returned an empty verify response.");

                return Result<PaystackVerifyResponse>.Success(data, "Paystack verify succeeded.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack verify threw for reference {Reference}.", reference);
                return Result<PaystackVerifyResponse>.Failure(ErrorCodes.Exception, $"Paystack verify failed. {ex.Message}");
            }
        }

        public bool VerifyWebhookSignature(string rawBody, string? signatureHeader)
        {
            if (!IsConfigured || string.IsNullOrEmpty(signatureHeader) || rawBody is null)
                return false;

            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(_settings.SecretKey);
                var bodyBytes = Encoding.UTF8.GetBytes(rawBody);

                using var hmac = new HMACSHA512(keyBytes);
                var computedHash = hmac.ComputeHash(bodyBytes);
                var computedHex = Convert.ToHexString(computedHash).ToLowerInvariant();
                var expected = signatureHeader.Trim().ToLowerInvariant();

                // Constant-time comparison to avoid timing attacks.
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHex),
                    Encoding.UTF8.GetBytes(expected));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paystack signature verification threw.");
                return false;
            }
        }

        /// <summary>
        /// Registered by DI as the typed <see cref="HttpClient"/> configurator.
        /// Centralises base URL + bearer auth so callers never handle credentials.
        /// </summary>
        public static void ConfigureHttpClient(HttpClient client, PaystackSettings settings)
        {
            var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl) ? "https://api.paystack.co" : settings.BaseUrl.TrimEnd('/');
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
