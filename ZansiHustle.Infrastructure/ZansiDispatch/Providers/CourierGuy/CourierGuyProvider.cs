using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.ZansiDispatch.Providers;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Shared.Enums.ZansiDispatch;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.ZansiDispatch.Providers.CourierGuy
{
    /// <summary>
    /// Courier Guy / Shiplogic provider behind the ZansiDispatch abstraction.
    /// Wraps the documented HTTP API (POST /rates, POST /shipments,
    /// GET /tracking/shipments, POST /shipments/cancel, GET /shipments/label).
    ///
    /// Security: the API key is read from config (never hard-coded), sent as a
    /// Bearer header per request, and NEVER serialised into request logs (we log
    /// only the JSON body, never headers). Raw responses are kept for ops
    /// debugging and never exposed to mobile by the service.
    ///
    /// Response mapping is DEFENSIVE — the sample response shapes for /rates and
    /// the create/track payloads aren't confirmed, so we parse JSON dynamically,
    /// try several likely field names, and tolerate missing fields. See the
    /// TODO(rate-shape) / TODO(shipment-shape) markers — finalise these once a
    /// sandbox response is captured.
    /// </summary>
    public sealed class CourierGuyProvider : IZansiDispatchQuoteProvider, IZansiDispatchShipmentProvider
    {
        private const string DefaultSandboxBaseUrl = "https://api.shiplogic.com";
        private const int MaxRawLogChars = 8000;

        private readonly HttpClient _http;
        private readonly ZansiDispatchOptions _opts;
        private readonly ILogger<CourierGuyProvider> _logger;

        public CourierGuyProvider(HttpClient http, IOptions<ZansiDispatchOptions> opts, ILogger<CourierGuyProvider> logger)
        {
            _http = http;
            _opts = opts.Value;
            _logger = logger;
        }

        public ZansiDispatchProviderType ProviderType => ZansiDispatchProviderType.CourierGuy;

        private ZansiDispatchCourierGuyOptions Cg => _opts.CourierGuy;

        public bool IsEnabled => Cg.Enabled && Cg.IsConfigured;

        private string BaseUrl => string.IsNullOrWhiteSpace(Cg.BaseUrl) ? DefaultSandboxBaseUrl : Cg.BaseUrl!.TrimEnd('/');

        // ── Quote (POST /rates) ─────────────────────────────────────────────

        public async Task<Result<ProviderQuoteResult>> GetQuoteOptionsAsync(
            ZansiDispatchQuoteContext ctx, ZansiDispatchSettings settings, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return Result<ProviderQuoteResult>.Failure(ErrorCodes.ProviderNotConfigured, "Courier Guy provider is not enabled/configured.");

            var body = new CgRatesRequest
            {
                CollectionAddress = BuildAddress(ctx, seller: true),
                DeliveryAddress = BuildAddress(ctx, seller: false),
                Parcels = new List<CgParcel> { BuildParcel(ctx) },
                DeclaredValue = ctx.DeclaredValue,
                CollectionMinDate = ctx.CollectionMinDate?.ToString("yyyy-MM-dd"),
                DeliveryMinDate = ctx.DeliveryMinDate?.ToString("yyyy-MM-dd"),
            };

            var (status, respBody, transportError) = await SendAsync(HttpMethod.Post, "/rates", body, ct);
            var result = new ProviderQuoteResult
            {
                RawRequestJson = Serialize(body),
                RawResponseJson = Truncate(respBody),
                StatusCode = status,
            };

            if (transportError is not null || status is null || status >= 400 || respBody is null)
            {
                result.Ok = false;
                result.ErrorMessage = transportError ?? ExtractError(respBody) ?? $"Courier Guy /rates failed (HTTP {status}).";
                return Result<ProviderQuoteResult>.Success(result);
            }

            // TODO(rate-shape): confirm the real /rates response shape from a
            // sandbox call and tighten this mapping. Until then we scan for a
            // "rates" array (or a root array) and read likely fields.
            try
            {
                result.Options = MapRates(respBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ZansiDispatch CourierGuy: rate mapping failed; treating as no options.");
                result.Options = new List<ProviderQuoteOption>();
            }

            if (result.Options.Count == 0)
            {
                result.Ok = false;
                result.ErrorMessage = "Courier Guy returned no usable rates.";
            }
            return Result<ProviderQuoteResult>.Success(result);
        }

        private static List<ProviderQuoteOption> MapRates(string respBody)
        {
            var options = new List<ProviderQuoteOption>();
            using var doc = JsonDocument.Parse(respBody);
            var root = doc.RootElement;

            JsonElement ratesArray;
            if (root.ValueKind == JsonValueKind.Array)
                ratesArray = root;
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("rates", out var r) && r.ValueKind == JsonValueKind.Array)
                ratesArray = r;
            else
                return options;

            foreach (var rate in ratesArray.EnumerateArray())
            {
                if (rate.ValueKind != JsonValueKind.Object) continue;

                // Amount: try several likely field names.
                var amount = GetDecimal(rate, "rate", "total", "charge", "base_rate", "rate_excluding_vat", "total_charge", "total_amount");
                if (amount is null || amount <= 0m) continue;

                var vat = GetDecimal(rate, "rate_vat", "vat", "vat_amount");
                var total = GetDecimal(rate, "total_charge", "total", "total_amount") ?? amount;

                // Service-level can be nested or flat.
                string? slCode = GetString(rate, "service_level_code", "code");
                string? slName = GetString(rate, "service_level_name", "name");
                string? slId = GetString(rate, "service_level_id", "id");
                if (rate.TryGetProperty("service_level", out var sl) && sl.ValueKind == JsonValueKind.Object)
                {
                    slCode ??= GetString(sl, "code");
                    slName ??= GetString(sl, "name");
                    slId ??= GetString(sl, "id");
                }

                var minDays = GetInt(rate, "min_days", "delivery_days_min", "estimated_delivery_days_min");
                var maxDays = GetInt(rate, "max_days", "delivery_days_max", "estimated_delivery_days_max");

                options.Add(new ProviderQuoteOption
                {
                    ProviderType = ZansiDispatchProviderType.CourierGuy,
                    ProviderServiceLevelId = slId,
                    ServiceLevelCode = slCode,
                    ServiceLevelName = slName,
                    ServiceLevel = ClassifyServiceLevel(slCode, slName),
                    Label = string.IsNullOrWhiteSpace(slName) ? "Courier Delivery" : slName!,
                    Description = "Delivery handled by ZansiHustle Dispatch (The Courier Guy)",
                    QuotedAmount = total ?? amount.Value,
                    VatAmount = vat,
                    TotalAmount = total,
                    Currency = "ZAR",
                    EstimatedDeliveryDaysMin = minDays,
                    EstimatedDeliveryDaysMax = maxDays,
                    RawProviderResponseJson = Truncate(rate.GetRawText()),
                });
            }

            return options;
        }

        // ── Create shipment (POST /shipments) ───────────────────────────────

        public async Task<Result<ProviderShipmentResult>> CreateShipmentAsync(ProviderShipmentRequest request, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return Result<ProviderShipmentResult>.Failure(ErrorCodes.ProviderNotConfigured, "Courier Guy provider is not enabled/configured.");

            var body = new CgShipmentRequest
            {
                CollectionAddress = ToCgAddress(request.CollectionAddress),
                CollectionContact = ToCgContact(request.CollectionContact),
                DeliveryAddress = ToCgAddress(request.DeliveryAddress),
                DeliveryContact = ToCgContact(request.DeliveryContact),
                Parcels = request.Parcels.Select(ToCgParcel).ToList(),
                DeclaredValue = request.DeclaredValue,
                CustomerReference = request.CustomerReference,
                CustomerReferenceName = request.CustomerReferenceName ?? "Order no.",
                SpecialInstructionsCollection = request.SpecialInstructionsCollection,
                SpecialInstructionsDelivery = request.SpecialInstructionsDelivery,
                CollectionMinDate = request.CollectionMinDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                DeliveryMinDate = request.DeliveryMinDate?.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ServiceLevelCode = request.ServiceLevelCode,
                ServiceLevelId = int.TryParse(request.ServiceLevelId, out var sid) ? sid : null,
                MuteNotifications = request.MuteNotifications,
            };

            var (status, respBody, transportError) = await SendAsync(HttpMethod.Post, "/shipments", body, ct);
            var result = new ProviderShipmentResult
            {
                RawRequestJson = Serialize(body),
                RawResponseJson = Truncate(respBody),
                StatusCode = status,
            };

            if (transportError is not null || status is null || status >= 400 || respBody is null)
            {
                result.Ok = false;
                // Map clean provider errors (insufficient funds, account closed,
                // invalid postal code, missing contacts, rate expired, etc.).
                result.ErrorMessage = transportError ?? ExtractError(respBody) ?? $"Courier Guy /shipments failed (HTTP {status}).";
                return Result<ProviderShipmentResult>.Success(result);
            }

            // TODO(shipment-shape): confirm the create-shipment response shape
            // from sandbox and tighten these field reads.
            try
            {
                using var doc = JsonDocument.Parse(respBody);
                var root = doc.RootElement.ValueKind == JsonValueKind.Object
                           && doc.RootElement.TryGetProperty("shipment", out var sh) && sh.ValueKind == JsonValueKind.Object
                    ? sh : doc.RootElement;

                result.ProviderShipmentId = GetString(root, "id", "shipment_id");
                result.ProviderShipmentReference = GetString(root, "reference", "shipment_reference", "provider_reference");
                result.TrackingNumber = GetString(root, "tracking_reference", "tracking_number", "waybill_number");
                result.ShortTrackingReference = GetString(root, "short_tracking_reference", "short_tracking_ref");
                result.ServiceLevelCode = GetString(root, "service_level_code");
                result.ServiceLevelName = GetString(root, "service_level_name");
                result.BookedCost = GetDecimal(root, "rate", "total", "charge", "total_charge");
                result.InitialProviderStatus = GetString(root, "status", "tracking_status");
                if (!string.IsNullOrWhiteSpace(result.InitialProviderStatus))
                    result.InitialStatus = CourierGuyStatusMap.Map(result.InitialProviderStatus);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ZansiDispatch CourierGuy: shipment response mapping partial.");
            }

            return Result<ProviderShipmentResult>.Success(result);
        }

        // ── Track (GET /tracking/shipments) ─────────────────────────────────

        public async Task<Result<ProviderTrackingResult>> GetShipmentStatusAsync(string trackingReference, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return Result<ProviderTrackingResult>.Failure(ErrorCodes.ProviderNotConfigured, "Courier Guy provider is not enabled/configured.");

            var path = $"/tracking/shipments?tracking_reference={Uri.EscapeDataString(trackingReference)}";
            var (status, respBody, transportError) = await SendAsync(HttpMethod.Get, path, null, ct);
            var result = new ProviderTrackingResult
            {
                RawResponseJson = Truncate(respBody),
                StatusCode = status,
            };

            if (transportError is not null || status is null || status >= 400 || respBody is null)
            {
                result.Ok = false;
                result.ErrorMessage = transportError ?? ExtractError(respBody) ?? $"Courier Guy tracking failed (HTTP {status}).";
                return Result<ProviderTrackingResult>.Success(result);
            }

            try
            {
                result.Events = MapTrackingEvents(respBody);
                if (result.Events.Count > 0)
                {
                    var latest = result.Events.OrderBy(e => e.EventTime).Last();
                    result.CurrentProviderStatus = latest.ProviderStatus;
                    result.CurrentStatus = latest.InternalStatus;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ZansiDispatch CourierGuy: tracking mapping failed.");
            }

            return Result<ProviderTrackingResult>.Success(result);
        }

        private static List<ProviderTrackingEvent> MapTrackingEvents(string respBody)
        {
            var events = new List<ProviderTrackingEvent>();
            using var doc = JsonDocument.Parse(respBody);
            var root = doc.RootElement;

            // TODO(tracking-shape): confirm the tracking response shape. We look
            // for an array under common keys, or a root array.
            JsonElement arr = default;
            var found = false;
            foreach (var key in new[] { "events", "tracking_events", "status_history", "history", "shipment_tracking_events" })
            {
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(key, out var e) && e.ValueKind == JsonValueKind.Array)
                {
                    arr = e; found = true; break;
                }
            }
            if (!found && root.ValueKind == JsonValueKind.Array) { arr = root; found = true; }
            if (!found) return events;

            foreach (var ev in arr.EnumerateArray())
            {
                if (ev.ValueKind != JsonValueKind.Object) continue;
                var providerStatus = GetString(ev, "status", "tracking_status", "state") ?? string.Empty;
                var time = GetDateTime(ev, "date", "timestamp", "time", "event_time", "created_at") ?? DateTime.UtcNow;
                events.Add(new ProviderTrackingEvent
                {
                    ProviderStatus = providerStatus,
                    InternalStatus = CourierGuyStatusMap.Map(providerStatus),
                    Message = GetString(ev, "message", "description", "note"),
                    Location = GetString(ev, "location", "hub", "city"),
                    EventTime = time,
                    ProviderEventId = GetString(ev, "id", "event_id"),
                    RawJson = Truncate(ev.GetRawText()),
                });
            }
            return events;
        }

        // ── Cancel (POST /shipments/cancel) ─────────────────────────────────

        public async Task<Result<ProviderCancelResult>> CancelShipmentAsync(string trackingReference, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return Result<ProviderCancelResult>.Failure(ErrorCodes.ProviderNotConfigured, "Courier Guy provider is not enabled/configured.");

            var body = new CgCancelRequest { TrackingReference = trackingReference };
            var (status, respBody, transportError) = await SendAsync(HttpMethod.Post, "/shipments/cancel", body, ct);
            var result = new ProviderCancelResult
            {
                RawResponseJson = Truncate(respBody),
                StatusCode = status,
            };

            // Cancel success is 200 or 204.
            if (transportError is not null || status is null || status >= 400)
            {
                result.Ok = false;
                result.ErrorMessage = transportError ?? ExtractError(respBody) ?? $"Courier Guy cancel failed (HTTP {status}).";
            }
            return Result<ProviderCancelResult>.Success(result);
        }

        // ── Label (GET /shipments/label) ────────────────────────────────────

        public async Task<Result<ProviderLabelResult>> GetShipmentLabelAsync(string providerShipmentId, CancellationToken ct = default)
        {
            if (!IsEnabled)
                return Result<ProviderLabelResult>.Failure(ErrorCodes.ProviderNotConfigured, "Courier Guy provider is not enabled/configured.");

            var path = $"/shipments/label?id={Uri.EscapeDataString(providerShipmentId)}";
            var (status, respBody, transportError) = await SendAsync(HttpMethod.Get, path, null, ct);
            var result = new ProviderLabelResult
            {
                RawResponseJson = Truncate(respBody),
                StatusCode = status,
            };

            if (transportError is not null || status is null || status >= 400 || respBody is null)
            {
                result.Ok = false;
                result.ErrorMessage = transportError ?? ExtractError(respBody) ?? $"Courier Guy label failed (HTTP {status}).";
                return Result<ProviderLabelResult>.Success(result);
            }

            try
            {
                using var doc = JsonDocument.Parse(respBody);
                var root = doc.RootElement;
                result.LabelUrl = GetString(root, "url", "label_url", "download_url", "signed_url")
                                  ?? (root.ValueKind == JsonValueKind.String ? root.GetString() : null);
                // Docs: signed URL expires in ~24h.
                result.ExpiresAt = DateTime.UtcNow.AddHours(24);
                if (string.IsNullOrWhiteSpace(result.LabelUrl))
                {
                    result.Ok = false;
                    result.ErrorMessage = "Courier Guy returned no label URL.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ZansiDispatch CourierGuy: label mapping failed.");
                result.Ok = false;
                result.ErrorMessage = "Could not read the label URL.";
            }
            return Result<ProviderLabelResult>.Success(result);
        }

        // ── HTTP plumbing ───────────────────────────────────────────────────

        /// <summary>
        /// Sends a request with the Bearer key (never logged) and returns
        /// (statusCode, responseBody, transportError). Network failures surface
        /// as a transportError string with a null status.
        /// </summary>
        private async Task<(int? status, string? body, string? transportError)> SendAsync(
            HttpMethod method, string path, object? jsonBody, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var req = new HttpRequestMessage(method, new Uri(BaseUrl + path));
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Cg.ApiKey);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (jsonBody is not null)
                {
                    var json = Serialize(jsonBody);
                    req.Content = new StringContent(json ?? "{}", Encoding.UTF8, "application/json");
                }

                using var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);
                sw.Stop();
                return ((int)resp.StatusCode, body, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                // Do not leak the API key — log a generic message only.
                _logger.LogWarning("ZansiDispatch CourierGuy transport error on {Method} {Path}: {Error}", method, Mask(path), ex.Message);
                return (null, null, "Could not reach the courier service. Please try again.");
            }
        }

        // ── Mapping helpers ─────────────────────────────────────────────────

        private CgAddress BuildAddress(ZansiDispatchQuoteContext ctx, bool seller) => seller
            ? new CgAddress
            {
                Type = (ctx.SellerAddressType ?? ZansiDispatchAddressType.Business).ToString().ToLowerInvariant(),
                Company = ctx.SellerCompany,
                StreetAddress = ctx.SellerStreetAddress,
                LocalArea = ctx.SellerLocalArea,
                City = ctx.SellerCity,
                Zone = ctx.SellerProvince,
                Country = string.IsNullOrWhiteSpace(ctx.SellerCountry) ? "ZA" : ctx.SellerCountry,
                Code = ctx.SellerPostalCode,
                Lat = ctx.SellerLat,
                Lng = ctx.SellerLng,
            }
            : new CgAddress
            {
                Type = (ctx.BuyerAddressType ?? ZansiDispatchAddressType.Residential).ToString().ToLowerInvariant(),
                Company = ctx.BuyerCompany,
                StreetAddress = ctx.BuyerStreetAddress,
                LocalArea = ctx.BuyerLocalArea,
                City = ctx.BuyerCity,
                Zone = ctx.BuyerProvince,
                Country = string.IsNullOrWhiteSpace(ctx.BuyerCountry) ? "ZA" : ctx.BuyerCountry,
                Code = ctx.BuyerPostalCode,
                Lat = ctx.BuyerLat,
                Lng = ctx.BuyerLng,
            };

        private static CgParcel BuildParcel(ZansiDispatchQuoteContext ctx) => new()
        {
            ParcelDescription = ctx.ParcelDescription,
            SubmittedLengthCm = ctx.SubmittedLengthCm ?? 30m,
            SubmittedWidthCm = ctx.SubmittedWidthCm ?? 25m,
            SubmittedHeightCm = ctx.SubmittedHeightCm ?? 10m,
            SubmittedWeightKg = ctx.EstimatedWeightKg ?? 2m,
        };

        private static CgAddress ToCgAddress(ProviderAddress a) => new()
        {
            Type = a.Type?.ToString().ToLowerInvariant(),
            Company = a.Company,
            StreetAddress = a.StreetAddress,
            LocalArea = a.LocalArea,
            City = a.City,
            Zone = a.Zone,
            Country = string.IsNullOrWhiteSpace(a.Country) ? "ZA" : a.Country,
            Code = a.Code,
            Lat = a.Lat,
            Lng = a.Lng,
        };

        private static CgContact ToCgContact(ProviderContact c) => new()
        {
            Name = c.Name,
            MobileNumber = c.MobileNumber,
            Email = c.Email,
        };

        private static CgParcel ToCgParcel(ProviderParcel p) => new()
        {
            ParcelDescription = p.Description,
            SubmittedLengthCm = p.SubmittedLengthCm ?? 30m,
            SubmittedWidthCm = p.SubmittedWidthCm ?? 25m,
            SubmittedHeightCm = p.SubmittedHeightCm ?? 10m,
            SubmittedWeightKg = p.SubmittedWeightKg ?? 2m,
        };

        private static ZansiDispatchServiceLevel ClassifyServiceLevel(string? code, string? name)
        {
            var v = $"{code} {name}".ToLowerInvariant();
            if (v.Contains("eco") || v.Contains("economy")) return ZansiDispatchServiceLevel.Economy;
            if (v.Contains("exp") || v.Contains("express") || v.Contains("ovn") || v.Contains("overnight") || v.Contains("flyer"))
                return ZansiDispatchServiceLevel.Express;
            return ZansiDispatchServiceLevel.Standard;
        }

        // ── JSON helpers (null-safe; falsy fields may be omitted) ───────────

        private static string? Serialize(object value)
        {
            try { return JsonSerializer.Serialize(value, CourierGuyJson.Options); }
            catch { return null; }
        }

        private static string? Truncate(string? s)
            => s is null ? null : (s.Length <= MaxRawLogChars ? s : s.Substring(0, MaxRawLogChars));

        private static string Mask(string s)
        {
            var q = s.IndexOf('?');
            return q >= 0 ? s.Substring(0, q) : s;
        }

        private static string? ExtractError(string? body)
        {
            if (string.IsNullOrWhiteSpace(body)) return null;
            try
            {
                using var doc = JsonDocument.Parse(body);
                return GetString(doc.RootElement, "message", "error", "detail", "error_message", "title");
            }
            catch { return null; }
        }

        private static string? GetString(JsonElement el, params string[] names)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            foreach (var n in names)
            {
                if (el.TryGetProperty(n, out var p))
                {
                    if (p.ValueKind == JsonValueKind.String) return p.GetString();
                    if (p.ValueKind == JsonValueKind.Number) return p.ToString();
                }
            }
            return null;
        }

        private static decimal? GetDecimal(JsonElement el, params string[] names)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            foreach (var n in names)
            {
                if (el.TryGetProperty(n, out var p))
                {
                    if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d)) return d;
                    if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var ds)) return ds;
                }
            }
            return null;
        }

        private static int? GetInt(JsonElement el, params string[] names)
        {
            var d = GetDecimal(el, names);
            return d is null ? null : (int)d.Value;
        }

        private static DateTime? GetDateTime(JsonElement el, params string[] names)
        {
            var s = GetString(el, names);
            return DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt) ? dt : (DateTime?)null;
        }
    }
}
