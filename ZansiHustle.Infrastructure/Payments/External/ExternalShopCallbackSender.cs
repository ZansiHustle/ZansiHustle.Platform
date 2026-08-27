using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Payments.External;

namespace ZansiHustle.Infrastructure.Payments.External
{
    /// <summary>
    /// Delivers the signed callback to an external shop's CallbackUrl. No
    /// fixed BaseAddress — target hosts vary per shop and are validated
    /// upstream (HTTPS-only, allow-listed) before this is ever called.
    /// </summary>
    public sealed class ExternalShopCallbackSender : IExternalShopCallbackSender
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExternalShopCallbackSender> _logger;

        public ExternalShopCallbackSender(HttpClient http, ILogger<ExternalShopCallbackSender> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error)> SendAsync(
            string callbackUrl,
            byte[] bodyBytes,
            string signatureHex,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var content = new ByteArrayContent(bodyBytes);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, callbackUrl) { Content = content };
                request.Headers.Add("X-ZansiHustle-Signature", signatureHex);

                using var response = await _http.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                    return (true, null);

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "[ExternalPayments][Callback] non-success {StatusCode} from {Url}: {Body}",
                    (int)response.StatusCode, callbackUrl, Truncate(body, 300));
                return (false, $"HTTP {(int)response.StatusCode}");
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("[ExternalPayments][Callback] TIMEOUT delivering to {Url}.", callbackUrl);
                return (false, "Timed out.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ExternalPayments][Callback] delivery threw for {Url}.", callbackUrl);
                return (false, $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value[..max] + "…";
        }
    }
}
