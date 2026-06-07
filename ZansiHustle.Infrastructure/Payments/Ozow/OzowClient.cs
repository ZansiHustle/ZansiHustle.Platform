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

        public System.Collections.Generic.IReadOnlyList<string> GetMissingFieldEnvVars()
        {
            // Missing creds first, then the hash service which itself depends
            // on PrivateKey (already covered in `_settings.GetMissingFieldEnvVars`).
            return _settings.GetMissingFieldEnvVars();
        }

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
                // ── Env-var trim guards ─────────────────────────────────
                // Trim everything from settings BEFORE assigning to the
                // request so a copy-paste-introduced trailing newline /
                // space silently doesn't poison either the wire body or
                // the hash. We log when a trim actually changed the value
                // so an operator can fix the env-var configuration.
                var siteCodeRaw   = _settings.SiteCode   ?? string.Empty;
                var siteCode      = siteCodeRaw.Trim();
                var successRaw    = _settings.SuccessUrl ?? string.Empty;
                var successUrl    = successRaw.Trim();
                var cancelRaw     = _settings.CancelUrl  ?? string.Empty;
                var cancelUrl     = cancelRaw.Trim();
                var errorRaw      = _settings.ErrorUrl   ?? string.Empty;
                var errorUrl      = errorRaw.Trim();
                var notifyRaw     = _settings.NotifyUrl  ?? string.Empty;
                var notifyUrl     = notifyRaw.Trim();
                var countryRaw    = _settings.CountryCode ?? "ZA";
                var countryCode   = countryRaw.Trim();
                var currencyRaw   = _settings.CurrencyCode ?? "ZAR";
                var currencyCode  = currencyRaw.Trim();

                if (siteCodeRaw  != siteCode)  _logger.LogWarning("[Ozow] SiteCode env var has surrounding whitespace.");
                if (successRaw   != successUrl) _logger.LogWarning("[Ozow] SuccessUrl env var has surrounding whitespace.");
                if (cancelRaw    != cancelUrl)  _logger.LogWarning("[Ozow] CancelUrl env var has surrounding whitespace.");
                if (errorRaw     != errorUrl)   _logger.LogWarning("[Ozow] ErrorUrl env var has surrounding whitespace.");
                if (notifyRaw    != notifyUrl)  _logger.LogWarning("[Ozow] NotifyUrl env var has surrounding whitespace.");

                request.SiteCode = siteCode;
                request.IsTest = _settings.IsTest;
                request.CountryCode = string.IsNullOrWhiteSpace(request.CountryCode) ? countryCode : request.CountryCode.Trim();
                request.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? currencyCode : request.CurrencyCode.Trim();
                request.SuccessUrl = string.IsNullOrWhiteSpace(request.SuccessUrl) ? successUrl : request.SuccessUrl.Trim();
                request.CancelUrl  = string.IsNullOrWhiteSpace(request.CancelUrl)  ? cancelUrl  : request.CancelUrl.Trim();
                request.ErrorUrl   = string.IsNullOrWhiteSpace(request.ErrorUrl)   ? errorUrl   : request.ErrorUrl.Trim();
                request.NotifyUrl  = string.IsNullOrWhiteSpace(request.NotifyUrl)  ? notifyUrl  : request.NotifyUrl.Trim();

                // Normalise the decimal scale to 2 places so the JSON wire
                // body and the hash input agree on the exact string form.
                //   • Math.Round(5m, 2) → 5m       (scale 0, JSON emits "5")
                //   • Math.Round(5m, 2) + 0.00m → 5.00m (scale 2, JSON emits "5.00")
                // OzowHashService formats the amount as "0.00" invariant for
                // the SHA512 input regardless, but pinning the scale here
                // keeps the wire body unambiguous and matches what other
                // Ozow integrations send. The numeric value is unchanged.
                request.Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero) + 0.00m;

                // Diagnostic version → returns the hash AND each field's
                // body-vs-hash value so we can log a sanitized comparison
                // and surface mismatches. PrivateKey never in the diagnostic.
                var (hash, diag) = _hashService.GenerateRequestHashWithDiagnostic(request);
                request.HashCheck = hash;

                // Pre-call comparison log. Every hashable field, both
                // forms. Anyone debugging "HashCheck value has failed" can
                // diff the body column vs the hash column in one glance.
                // Truncates long values (e.g., URLs over 80 chars) for the
                // sanitized line; the hash precheck below catches divergences.
                foreach (var f in diag.Fields)
                {
                    _logger.LogInformation(
                        "[Ozow][HashFields] field={Field} body={Body} hash={Hash}",
                        f.Name, Truncate(f.BodyValue, 80), Truncate(f.HashValue, 80));
                }

                _logger.LogInformation(
                    "[Ozow][HashGuard] ref={Ref} privateKeyPresent=True privateKeyLength={Len} " +
                    "privateKeyTrimChanged={PkTrim} siteCodeTrimChanged={ScTrim} anyUrlTrimChanged={UrlTrim}",
                    request.TransactionReference,
                    diag.PrivateKeyLength,
                    diag.PrivateKeyTrimChanged,
                    diag.SiteCodeTrimChanged,
                    diag.AnyUrlTrimChanged);

                // Hard precheck: any divergence between body string and
                // hash string for ANY field is a bug. Loud warning per
                // field so it can't be missed in stdout.
                if (diag.AnyMismatch)
                {
                    foreach (var f in diag.Fields)
                    {
                        if (!string.Equals(f.BodyValue, f.HashValue, StringComparison.Ordinal))
                        {
                            _logger.LogError(
                                "[Ozow][HashMismatchPrecheck] field={Field} body={Body} hash={Hash}",
                                f.Name, f.BodyValue, f.HashValue);
                        }
                    }
                }

                // Pre-call log. Never log ApiKey/PrivateKey/HashCheck — only
                // the safe call-context fields. UAT debugging happens against
                // the deployed API, so logs need to be informative enough to
                // diagnose remotely without leaking credentials.
                _logger.LogInformation(
                    "[Ozow] PostPaymentRequest → ref={Ref} amount={Amount} {Currency} site={Site} isTest={IsTest} bankRef={BankRef}",
                    request.TransactionReference, request.Amount, request.CurrencyCode,
                    request.SiteCode, request.IsTest, request.BankReference);

                // Stopwatch + log start/done. Pair with the matching
                // PaymentService.InitializeOzowAsync trace so a 502 from
                // Cloudflare always has an origin-side line you can grep for.
                var sw = System.Diagnostics.Stopwatch.StartNew();
                _logger.LogInformation(
                    "[Ozow] PostPaymentRequest START ref={Ref} baseHost={Host} path={Path}",
                    request.TransactionReference,
                    _http.BaseAddress?.Host ?? "<unset>",
                    "/PostPaymentRequest");

                using var response = await _http.PostAsJsonAsync("/PostPaymentRequest", request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(cancellationToken);
                    // Truncate the provider body for the log — Ozow's error
                    // page can be HTML and bloats logs without adding info.
                    var safeBody = text.Length > 512 ? text[..512] + "…" : text;
                    _logger.LogError(
                        "[Ozow] PostPaymentRequest FAILED status={StatusCode} ref={Ref} elapsedMs={Elapsed} body={Body}",
                        (int)response.StatusCode, request.TransactionReference, sw.ElapsedMilliseconds, safeBody);
                    return Result<OzowPaymentRequestResult>.Failure(
                        ErrorCodes.PaymentProviderUnavailable,
                        $"Ozow returned HTTP {(int)response.StatusCode}.");
                }

                var data = await response.Content.ReadFromJsonAsync<OzowPaymentRequestResult>(cancellationToken: cancellationToken);

                if (data is null || string.IsNullOrWhiteSpace(data.Url) || string.IsNullOrWhiteSpace(data.PaymentRequestId))
                {
                    _logger.LogWarning(
                        "[Ozow] PostPaymentRequest returned non-actionable payload ref={Ref} elapsedMs={Elapsed} error={Error}",
                        request.TransactionReference, sw.ElapsedMilliseconds, data?.ErrorMessage);
                    return Result<OzowPaymentRequestResult>.Failure(
                        ErrorCodes.PaymentInitFailed,
                        data?.ErrorMessage ?? "Ozow returned an empty response.");
                }

                _logger.LogInformation(
                    "[Ozow] PostPaymentRequest OK ref={Ref} paymentRequestId={Prid} elapsedMs={Elapsed}",
                    request.TransactionReference, data.PaymentRequestId, sw.ElapsedMilliseconds);

                return Result<OzowPaymentRequestResult>.Success(data, "Ozow PostPaymentRequest succeeded.");
            }
            catch (TaskCanceledException tcex)
            {
                // HttpClient cancellation = network timeout (the 30s in
                // ConfigureHttpClient) OR caller cancellation. Either way the
                // provider was unreachable for this attempt, not declining us.
                _logger.LogError(tcex,
                    "[Ozow] PostPaymentRequest TIMEOUT ref={Ref}.",
                    request.TransactionReference);
                return Result<OzowPaymentRequestResult>.Failure(
                    ErrorCodes.PaymentProviderUnavailable,
                    "Ozow did not respond in time. Please try again.");
            }
            catch (HttpRequestException hrex)
            {
                _logger.LogError(hrex,
                    "[Ozow] PostPaymentRequest NETWORK-FAILED ref={Ref}.",
                    request.TransactionReference);
                return Result<OzowPaymentRequestResult>.Failure(
                    ErrorCodes.PaymentProviderUnavailable,
                    "Couldn't reach Ozow. Please try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Ozow] PostPaymentRequest THREW ref={Ref}.", request.TransactionReference);
                return Result<OzowPaymentRequestResult>.Failure(
                    ErrorCodes.PaymentProviderUnavailable,
                    "Ozow request failed unexpectedly.");
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

        // Truncate long values (URLs) for the sanitized comparison log so
        // a single field doesn't blow up the line. Hashing uses the full
        // value — only the LOG is truncated.
        private static string Truncate(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value[..max] + "…";
        }
    }
}
