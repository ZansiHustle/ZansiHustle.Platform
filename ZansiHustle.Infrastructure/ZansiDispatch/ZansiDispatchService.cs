using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Application.ZansiDispatch.Dtos;
using ZansiHustle.Application.ZansiDispatch.Providers;
using ZansiHustle.Domain.ZansiDispatch;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.ZansiDispatch;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.ZansiDispatch
{
    /// <summary>
    /// ZansiDispatch service. Talks to <see cref="AppDbContext"/> directly and
    /// resolves quote/shipment providers through the abstraction — Courier Guy
    /// (Shiplogic) is Provider #1 with InternalEstimate as the deterministic
    /// fallback. Application/order/mobile code never sees provider payloads.
    ///
    /// Backend is the single source of truth for delivery pricing. Provider
    /// failures are mapped into clean <see cref="Result"/> envelopes; raw
    /// responses are logged for ops but never returned to mobile, and API keys
    /// are never logged (the provider masks them).
    /// </summary>
    public sealed class ZansiDispatchService : IZansiDispatchService
    {
        private readonly AppDbContext _db;
        private readonly IEnumerable<IZansiDispatchQuoteProvider> _quoteProviders;
        private readonly IEnumerable<IZansiDispatchShipmentProvider> _shipmentProviders;
        private readonly ZansiDispatchOptions _opts;
        private readonly DispatchDebugOptions _debug;
        private readonly ILogger<ZansiDispatchService> _logger;

        public ZansiDispatchService(
            AppDbContext db,
            IEnumerable<IZansiDispatchQuoteProvider> quoteProviders,
            IEnumerable<IZansiDispatchShipmentProvider> shipmentProviders,
            IOptions<ZansiDispatchOptions> opts,
            IOptions<DispatchDebugOptions> debug,
            ILogger<ZansiDispatchService> logger)
        {
            _db = db;
            _quoteProviders = quoteProviders;
            _shipmentProviders = shipmentProviders;
            _opts = opts.Value;
            _debug = debug.Value;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════════
        // Quoting
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<QuoteDto>> CreateQuoteAsync(Guid userId, CreateQuoteRequestDto request, CancellationToken ct = default, string? correlationId = null)
        {
            var cid = string.IsNullOrWhiteSpace(correlationId) ? $"dq-{Guid.NewGuid():N}" : correlationId.Trim();
            try
            {
                if (request is null)
                    return Result<QuoteDto>.Failure(ErrorCodes.BadRequest, "Request is required.");

                if (_debug.Enabled)
                {
                    _logger.LogInformation(
                        "[DispatchQuoteDebug] Incoming quote request correlationId={Cid} userId={UserId} listingId={ListingId} merchantId={MerchantId} destination={Destination} parcel={Parcel} request={Request}",
                        cid, userId, request.ListingId, request.MerchantId ?? request.SellerId,
                        Redact(request.BuyerAddressSummary),
                        $"size={request.ItemSizeCategory} weightKg={request.EstimatedWeightKg} declaredValue={request.DeclaredValue}",
                        Redact(SafeJson(request)));
                }

                var settings = ZansiDispatchDefaults.Resolve(await LoadSettingsAsync(ct));
                var now = DateTime.UtcNow;

                var merchantId = request.MerchantId ?? request.SellerId;
                var (sellerProvince, sellerCity, sellerAddress, sellerStreet, sellerLocal, sellerPostal, sellerCountry, sellerLat, sellerLng) =
                    await ResolveSellerAddressAsync(request, merchantId, ct);

                // Seller-origin guard. A courier/internal quote needs a pickup
                // origin. When the seller address wasn't supplied AND can't be
                // resolved from the merchant (no province/city), DON'T fabricate a
                // quote — return a friendly "unavailable" so the buyer can't be
                // pushed into an unfulfillable order. Manual quotes (Portal /
                // standalone) supply the origin, so they're unaffected.
                // Dev reasonCode: SELLER_ORIGIN_MISSING.
                if (string.IsNullOrWhiteSpace(sellerProvince) && string.IsNullOrWhiteSpace(sellerCity))
                {
                    _logger.LogWarning(
                        "ZansiDispatch quote unavailable reason=SELLER_ORIGIN_MISSING merchantId={MerchantId} listingId={ListingId} userId={UserId}",
                        merchantId, request.ListingId, userId);
                    return Result<QuoteDto>.Failure(
                        ErrorCodes.BadRequest, "Delivery is not available for this item yet.");
                }

                var context = new ZansiDispatchQuoteContext
                {
                    ListingId = request.ListingId,
                    ShopId = request.ShopId,
                    MerchantId = merchantId,
                    BuyerCompany = Trim(request.BuyerCompany),
                    BuyerAddressType = request.BuyerAddressType,
                    BuyerStreetAddress = Trim(request.BuyerStreetAddress),
                    BuyerLocalArea = Trim(request.BuyerLocalArea),
                    BuyerCity = Trim(request.BuyerCity),
                    BuyerProvince = Trim(request.BuyerProvince),
                    BuyerCountry = Trim(request.BuyerCountry) ?? "ZA",
                    BuyerPostalCode = Trim(request.BuyerPostalCode),
                    BuyerLat = request.BuyerLat,
                    BuyerLng = request.BuyerLng,
                    BuyerAddressSummary = Trim(request.BuyerAddressSummary),
                    SellerCompany = Trim(request.SellerCompany),
                    SellerAddressType = request.SellerAddressType,
                    SellerStreetAddress = sellerStreet,
                    SellerLocalArea = sellerLocal,
                    SellerCity = sellerCity,
                    SellerProvince = sellerProvince,
                    SellerCountry = sellerCountry ?? "ZA",
                    SellerPostalCode = sellerPostal,
                    SellerLat = sellerLat,
                    SellerLng = sellerLng,
                    SellerAddressSummary = sellerAddress,
                    ParcelDescription = Trim(request.ParcelDescription),
                    ItemSizeCategory = request.ItemSizeCategory,
                    EstimatedWeightKg = request.EstimatedWeightKg,
                    SubmittedLengthCm = request.SubmittedLengthCm,
                    SubmittedWidthCm = request.SubmittedWidthCm,
                    SubmittedHeightCm = request.SubmittedHeightCm,
                    DistanceKm = request.DistanceKm,
                    DeclaredValue = request.DeclaredValue,
                    CollectionMinDate = request.CollectionMinDate,
                    DeliveryMinDate = request.DeliveryMinDate,
                };

                if (_debug.Enabled)
                {
                    // The DERIVED quote input — what the provider actually prices
                    // against (seller/pickup resolved from the merchant, parcel
                    // defaults applied), which the thin mobile request doesn't show.
                    _logger.LogInformation(
                        "[DispatchQuoteDebug] Resolved quote context correlationId={Cid} listingId={ListingId} merchantId={MerchantId} shopId={ShopId} " +
                        "pickup={Pickup} dropoff={Dropoff} parcelSize={Size} weightKg={WeightKg} dims={Dims} declaredValue={DeclaredValue} " +
                        "collectionIncluded={Collection} context={Context}",
                        cid, context.ListingId, context.MerchantId, context.ShopId,
                        Redact($"{context.SellerLocalArea} / {context.SellerCity} / {context.SellerProvince} / {context.SellerPostalCode} / {context.SellerAddressSummary}"),
                        Redact($"{context.BuyerLocalArea} / {context.BuyerCity} / {context.BuyerProvince} / {context.BuyerPostalCode} / {context.BuyerAddressSummary}"),
                        context.ItemSizeCategory, context.EstimatedWeightKg,
                        $"{context.SubmittedLengthCm}x{context.SubmittedWidthCm}x{context.SubmittedHeightCm}",
                        context.DeclaredValue,
                        settings.CollectionEnabled && (request.IncludeCollectionOption ?? true),
                        Redact(SafeJson(context)));
                }

                var (providerOptions, providerUsed, fallbackUsed) = await ResolveQuoteAsync(context, settings, ct, cid);

                var quote = new ZansiDispatchQuote
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ListingId = request.ListingId,
                    ShopId = request.ShopId,
                    MerchantId = merchantId,
                    BuyerProvince = context.BuyerProvince,
                    BuyerCity = context.BuyerCity,
                    BuyerAddressSummary = context.BuyerAddressSummary,
                    BuyerStreetAddress = context.BuyerStreetAddress,
                    BuyerLocalArea = context.BuyerLocalArea,
                    BuyerPostalCode = context.BuyerPostalCode,
                    BuyerCountry = context.BuyerCountry,
                    BuyerLat = context.BuyerLat,
                    BuyerLng = context.BuyerLng,
                    BuyerAddressType = context.BuyerAddressType,
                    SellerProvince = context.SellerProvince,
                    SellerCity = context.SellerCity,
                    SellerAddressSummary = context.SellerAddressSummary,
                    SellerStreetAddress = context.SellerStreetAddress,
                    SellerLocalArea = context.SellerLocalArea,
                    SellerPostalCode = context.SellerPostalCode,
                    SellerCountry = context.SellerCountry,
                    SellerLat = context.SellerLat,
                    SellerLng = context.SellerLng,
                    SellerAddressType = context.SellerAddressType,
                    ParcelDescription = context.ParcelDescription,
                    ItemSizeCategory = request.ItemSizeCategory,
                    EstimatedWeightKg = request.EstimatedWeightKg,
                    SubmittedLengthCm = request.SubmittedLengthCm,
                    SubmittedWidthCm = request.SubmittedWidthCm,
                    SubmittedHeightCm = request.SubmittedHeightCm,
                    DistanceKm = request.DistanceKm,
                    DeclaredValue = request.DeclaredValue,
                    Status = ZansiDispatchQuoteStatus.Presented,
                    ExpiresAt = now.AddMinutes(settings.QuoteExpiryMinutes),
                    CreatedAt = now,
                    UpdatedAt = now,
                };

                foreach (var po in providerOptions)
                    quote.Options.Add(MapProviderOption(quote.Id, po, now));

                if (settings.CollectionEnabled && (request.IncludeCollectionOption ?? true))
                {
                    quote.Options.Add(new ZansiDispatchQuoteOption
                    {
                        Id = Guid.NewGuid(),
                        QuoteId = quote.Id,
                        ProviderType = ZansiDispatchProviderType.InternalEstimate,
                        ServiceLevel = ZansiDispatchServiceLevel.Collection,
                        Label = "Arrange Collection",
                        Description = "No delivery fee. Buyer and seller arrange collection.",
                        QuotedAmount = 0m,
                        Currency = "ZAR",
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }

                if (quote.Options.Count == 0)
                    return Result<QuoteDto>.Failure(ErrorCodes.Exception, "No delivery options could be generated. Please try again or choose collection.");

                _db.ZansiDispatchQuotes.Add(quote);
                await _db.SaveChangesAsync(ct);

                var dto = MapQuote(quote);
                dto.ProviderUsed = providerUsed;
                dto.FallbackUsed = fallbackUsed;
                dto.PricingSource = fallbackUsed
                    ? $"{providerUsed} (fallback)"
                    : providerUsed.ToString();

                if (_debug.Enabled)
                {
                    // Human-readable enum names alongside the ints so the log is
                    // self-explanatory (providerUsed=3 → CourierGuy, status=2 → Presented).
                    _logger.LogInformation(
                        "[DispatchQuoteDebug] Mapped quote response correlationId={Cid} quoteId={QuoteId} providerUsed={Provider} providerUsedName={ProviderName} status={Status} statusName={StatusName} fallbackUsed={Fallback} pricingSource={PricingSource} optionCount={Count} response={Response}",
                        cid, dto.QuoteId, (int)providerUsed, providerUsed.ToString(), (int)dto.Status, dto.Status.ToString(),
                        fallbackUsed, dto.PricingSource, dto.Options.Count, Redact(SafeJson(dto)));
                }

                return Result<QuoteDto>.Success(dto, "Delivery options ready.");
            }
            catch (Exception ex)
            {
                if (_debug.Enabled)
                {
                    _logger.LogWarning(
                        "[DispatchQuoteDebug] Quote failed correlationId={Cid} userId={UserId} exceptionType={ExType} error={Error}",
                        cid, userId, ex.GetType().Name, ex.Message);
                }
                _logger.LogError(ex, "ZansiDispatch CreateQuote failed. UserId={UserId}", userId);
                return Result<QuoteDto>.Failure(ErrorCodes.Exception, "Could not generate delivery options.");
            }
        }

        private async Task<(string? province, string? city, string? summary, string? street, string? local, string? postal, string? country, decimal? lat, decimal? lng)>
            ResolveSellerAddressAsync(CreateQuoteRequestDto request, Guid? merchantId, CancellationToken ct)
        {
            string? province = Trim(request.SellerProvince);
            string? city = Trim(request.SellerCity);
            string? summary = Trim(request.SellerAddressSummary);
            string? street = Trim(request.SellerStreetAddress);
            string? local = Trim(request.SellerLocalArea);
            string? postal = Trim(request.SellerPostalCode);
            string? country = Trim(request.SellerCountry);
            decimal? lat = request.SellerLat;
            decimal? lng = request.SellerLng;

            // Derive from the merchant when the seller address wasn't supplied.
            if (string.IsNullOrWhiteSpace(province) && string.IsNullOrWhiteSpace(city))
            {
                var mid = merchantId;
                if (mid is null && request.ListingId is Guid lid)
                {
                    mid = await _db.Listings.AsNoTracking()
                        .Where(l => l.Id == lid).Select(l => (Guid?)l.MerchantId).FirstOrDefaultAsync(ct);
                }
                if (mid is Guid m)
                {
                    var row = await _db.Merchants.AsNoTracking()
                        .Where(x => x.Id == m)
                        .Select(x => new { x.Province, x.City, x.FormattedAddress, x.AddressLine1, x.Suburb, x.PostalCode, x.CountryCode, x.Latitude, x.Longitude })
                        .FirstOrDefaultAsync(ct);
                    if (row is not null)
                    {
                        province ??= row.Province;
                        city ??= row.City;
                        summary ??= row.FormattedAddress;
                        street ??= row.AddressLine1;
                        local ??= row.Suburb;
                        postal ??= row.PostalCode;
                        country ??= row.CountryCode;
                        lat ??= row.Latitude;
                        lng ??= row.Longitude;
                    }
                }
            }

            return (province, city, summary, street, local, postal, country, lat, lng);
        }

        /// <summary>
        /// Resolve quote options: try the configured default provider; if it's
        /// disabled / can't quote and fallback is enabled, use InternalEstimate.
        /// Persists a provider-request log per HTTP attempt (saved with the quote).
        /// </summary>
        private async Task<(List<ProviderQuoteOption> options, ZansiDispatchProviderType providerUsed, bool fallbackUsed)>
            ResolveQuoteAsync(ZansiDispatchQuoteContext context, ZansiDispatchSettings settings, CancellationToken ct, string? cid = null)
        {
            var quoteByType = _quoteProviders.GroupBy(p => p.ProviderType).ToDictionary(g => g.Key, g => g.First());
            var desired = EffectiveDefaultProvider(settings);

            // Primary attempt.
            if (quoteByType.TryGetValue(desired, out var primary) && primary.IsEnabled)
            {
                var res = await RunQuoteProviderAsync(primary, context, settings, ct, cid);
                if (res is { Ok: true, Options.Count: > 0 })
                    return (res.Options, desired, false);
            }

            // Fallback to the deterministic internal estimate.
            var fallbackAllowed = settings.FallbackToInternalEstimate || settings.AllowManualFallback;
            if (fallbackAllowed
                && desired != ZansiDispatchProviderType.InternalEstimate
                && quoteByType.TryGetValue(ZansiDispatchProviderType.InternalEstimate, out var internalEstimate)
                && internalEstimate.IsEnabled)
            {
                var fb = await RunQuoteProviderAsync(internalEstimate, context, settings, ct, cid);
                if (fb is { Ok: true, Options.Count: > 0 })
                    return (fb.Options, ZansiDispatchProviderType.InternalEstimate, true);
            }

            return (new List<ProviderQuoteOption>(), desired, false);
        }

        private ZansiDispatchProviderType EffectiveDefaultProvider(ZansiDispatchSettings settings)
            => Enum.TryParse<ZansiDispatchProviderType>(_opts.DefaultProvider, ignoreCase: true, out var cfg)
               && Enum.IsDefined(typeof(ZansiDispatchProviderType), cfg)
                ? cfg : settings.DefaultProvider;

        private async Task<ProviderQuoteResult?> RunQuoteProviderAsync(
            IZansiDispatchQuoteProvider provider, ZansiDispatchQuoteContext context, ZansiDispatchSettings settings, CancellationToken ct, string? cid = null)
        {
            var sw = Stopwatch.StartNew();
            Result<ProviderQuoteResult> result;
            try { result = await provider.GetQuoteOptionsAsync(context, settings, ct); }
            catch (Exception ex) { result = Result<ProviderQuoteResult>.Failure(ErrorCodes.Exception, ex.Message); }
            sw.Stop();

            var data = result.IsSuccess ? result.Data : null;
            var reqJson = data?.RawRequestJson ?? SafeJson(context);
            var error = data?.Ok == false ? data.ErrorMessage : (result.IsSuccess ? null : result.Message);
            AddProviderLog(provider.ProviderType, ZansiDispatchProviderOperation.GetRates,
                requestJson: reqJson,
                responseJson: data?.RawResponseJson,
                ok: data?.Ok ?? false,
                error: error,
                statusCode: data?.StatusCode,
                durationMs: (int)sw.ElapsedMilliseconds);

            if (_debug.Enabled)
            {
                // Provider outbound + inbound payloads. For InternalEstimate (no
                // HTTP) the "request" is the quote context and the response is the
                // deterministic estimate; for CourierGuy these are the real /rates
                // payloads. API keys live in HTTP headers (never serialized here)
                // and are redacted defensively regardless.
                _logger.LogInformation(
                    "[DispatchQuoteDebug] Provider quote request correlationId={Cid} provider={Provider} enabled={Enabled} payload={Payload}",
                    cid, provider.ProviderType, provider.IsEnabled, Redact(reqJson));
                _logger.LogInformation(
                    "[DispatchQuoteDebug] Provider quote response correlationId={Cid} provider={Provider} ok={Ok} statusCode={Status} durationMs={Ms} error={Error} response={Response}",
                    cid, provider.ProviderType, data?.Ok ?? false, data?.StatusCode, (int)sw.ElapsedMilliseconds,
                    error, Redact(data?.RawResponseJson));
            }
            return data;
        }

        public async Task<Result<QuoteDto>> SelectOptionAsync(Guid userId, bool isAdmin, Guid quoteId, Guid quoteOptionId, CancellationToken ct = default)
        {
            try
            {
                var quote = await _db.ZansiDispatchQuotes.Include(q => q.Options).FirstOrDefaultAsync(q => q.Id == quoteId, ct);
                if (quote is null) return Result<QuoteDto>.Failure(ErrorCodes.NotFound, "Quote not found.");
                if (!isAdmin && quote.UserId != userId) return Result<QuoteDto>.Failure(ErrorCodes.Forbidden, "This quote belongs to another user.");

                var now = DateTime.UtcNow;
                if (quote.ExpiresAt is DateTime exp && exp < now)
                {
                    quote.Status = ZansiDispatchQuoteStatus.Expired;
                    quote.UpdatedAt = now;
                    await _db.SaveChangesAsync(ct);
                    return Result<QuoteDto>.Failure(ErrorCodes.BadRequest, "This quote has expired. Please request delivery options again.");
                }

                var option = quote.Options.FirstOrDefault(o => o.Id == quoteOptionId);
                if (option is null) return Result<QuoteDto>.Failure(ErrorCodes.NotFound, "That delivery option is not part of this quote.");

                foreach (var o in quote.Options)
                {
                    var sel = o.Id == quoteOptionId;
                    if (o.IsSelected != sel) { o.IsSelected = sel; o.UpdatedAt = now; }
                }
                quote.Status = ZansiDispatchQuoteStatus.Selected;
                quote.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
                return Result<QuoteDto>.Success(MapQuote(quote), "Delivery option selected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch SelectOption failed. QuoteId={QuoteId}", quoteId);
                return Result<QuoteDto>.Failure(ErrorCodes.Exception, "Could not select the delivery option.");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Order-creation bridge
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<SelectableQuoteOptionDto>> GetSelectableOptionAsync(Guid userId, Guid quoteOptionId, CancellationToken ct = default)
        {
            try
            {
                var option = await _db.ZansiDispatchQuoteOptions.Include(o => o.Quote).FirstOrDefaultAsync(o => o.Id == quoteOptionId, ct);
                if (option is null || option.Quote is null)
                    return Result<SelectableQuoteOptionDto>.Failure(ErrorCodes.NotFound, "Delivery option not found.");

                var quote = option.Quote;
                if (quote.UserId != userId)
                    return Result<SelectableQuoteOptionDto>.Failure(ErrorCodes.Forbidden, "This delivery option belongs to another user.");
                if (quote.ExpiresAt is DateTime exp && exp < DateTime.UtcNow)
                    return Result<SelectableQuoteOptionDto>.Failure(ErrorCodes.BadRequest, "Your delivery quote expired. Please request delivery options again.");

                return Result<SelectableQuoteOptionDto>.Success(new SelectableQuoteOptionDto
                {
                    QuoteId = quote.Id,
                    QuoteOptionId = option.Id,
                    ProviderType = option.ProviderType,
                    ServiceLevel = option.ServiceLevel,
                    Amount = option.QuotedAmount,
                    Currency = option.Currency,
                    MerchantId = quote.MerchantId,
                    ShopId = quote.ShopId,
                    PickupAddressSummary = quote.SellerAddressSummary,
                    DropoffAddressSummary = quote.BuyerAddressSummary,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch GetSelectableOption failed. OptionId={OptionId}", quoteOptionId);
                return Result<SelectableQuoteOptionDto>.Failure(ErrorCodes.Exception, "Could not validate the delivery option.");
            }
        }

        public async Task CreateShipmentForOrderAsync(CreateShipmentForOrderInput input, CancellationToken ct = default)
        {
            try
            {
                if (await _db.ZansiDispatchShipments.AnyAsync(s => s.OrderId == input.OrderId, ct)) return;

                var now = DateTime.UtcNow;
                var option = await _db.ZansiDispatchQuoteOptions.AsNoTracking().FirstOrDefaultAsync(o => o.Id == input.QuoteOptionId, ct);

                var shipment = new ZansiDispatchShipment
                {
                    Id = Guid.NewGuid(),
                    OrderId = input.OrderId,
                    QuoteId = input.QuoteId,
                    QuoteOptionId = input.QuoteOptionId,
                    UserId = input.UserId,
                    MerchantId = input.MerchantId,
                    ShopId = input.ShopId,
                    ProviderType = input.ProviderType,
                    ServiceLevel = input.ServiceLevel,
                    ServiceLevelCode = option?.ServiceLevelCode,
                    ServiceLevelName = option?.ServiceLevelName,
                    QuotedDeliveryFee = input.QuotedDeliveryFee,
                    Status = ZansiDispatchShipmentStatus.PendingDispatch,
                    ReconciliationStatus = ZansiDispatchReconciliationStatus.Pending,
                    PickupAddressSummary = input.PickupAddressSummary,
                    DropoffAddressSummary = input.DropoffAddressSummary,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                _db.ZansiDispatchShipments.Add(shipment);
                _db.ZansiDispatchLedgerEntries.Add(QuoteChargedLedger(shipment.Id, input.OrderId, input.QuotedDeliveryFee, input.UserId, now));

                var quote = await _db.ZansiDispatchQuotes.FirstOrDefaultAsync(q => q.Id == input.QuoteId, ct);
                if (quote is not null)
                {
                    quote.Status = ZansiDispatchQuoteStatus.ConvertedToOrder;
                    quote.OrderId = input.OrderId;
                    quote.UpdatedAt = now;
                }
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch CreateShipmentForOrder failed. OrderId={OrderId}", input.OrderId);
            }
        }

        /// <inheritdoc />
        public async Task CreateShipmentForPaidOrderAsync(
            Guid orderId, Guid userId, Guid? merchantId, Guid quoteOptionId,
            decimal quotedDeliveryFee, string? dropoffAddressSummary, CancellationToken ct = default)
        {
            try
            {
                if (orderId == Guid.Empty || quoteOptionId == Guid.Empty) return;

                // Idempotency #1: a shipment already exists for this order → no-op.
                // (CreateShipmentForOrderAsync re-checks this too, so repeated
                // payment signals / webhook retries can never duplicate.)
                if (await _db.ZansiDispatchShipments.AnyAsync(s => s.OrderId == orderId, ct)) return;

                // Resolve provider/service/addresses from the stored option WITHOUT
                // an expiry re-check — payment can settle after the quote's short
                // TTL, and the fee was already locked onto the order at creation.
                var option = await _db.ZansiDispatchQuoteOptions.AsNoTracking()
                    .Include(o => o.Quote)
                    .FirstOrDefaultAsync(o => o.Id == quoteOptionId, ct);
                if (option is null)
                {
                    _logger.LogWarning(
                        "ZansiDispatch CreateShipmentForPaidOrder: quote option {OptionId} not found for order {OrderId} — shipment not created.",
                        quoteOptionId, orderId);
                    return;
                }

                await CreateShipmentForOrderAsync(new CreateShipmentForOrderInput
                {
                    OrderId = orderId,
                    UserId = userId,
                    MerchantId = merchantId,
                    ShopId = option.Quote?.ShopId,
                    QuoteId = option.QuoteId,
                    QuoteOptionId = option.Id,
                    ProviderType = option.ProviderType,
                    ServiceLevel = option.ServiceLevel,
                    QuotedDeliveryFee = quotedDeliveryFee,
                    PickupAddressSummary = option.Quote?.SellerAddressSummary,
                    DropoffAddressSummary = dropoffAddressSummary ?? option.Quote?.BuyerAddressSummary,
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch CreateShipmentForPaidOrder failed. OrderId={OrderId}", orderId);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Shipment lifecycle (provider-backed)
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<ShipmentDto>> CreateShipmentFromQuoteAsync(Guid adminUserId, CreateShipmentFromQuoteRequestDto request, CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.OrderId == Guid.Empty || request.QuoteOptionId == Guid.Empty)
                    return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, "OrderId and QuoteOptionId are required.");

                var option = await _db.ZansiDispatchQuoteOptions.Include(o => o.Quote).FirstOrDefaultAsync(o => o.Id == request.QuoteOptionId, ct);
                if (option is null || option.Quote is null)
                    return Result<ShipmentDto>.Failure(ErrorCodes.NotFound, "Delivery option not found.");
                var quote = option.Quote;

                var order = await _db.Orders.AsNoTracking()
                    .Where(o => o.Id == request.OrderId)
                    .Select(o => new { o.Id, o.Code, o.BuyerUserId, o.MerchantId, o.PaymentStatus, o.Status, o.DeliveryQuoteOptionId })
                    .FirstOrDefaultAsync(ct);
                if (order is null) return Result<ShipmentDto>.Failure(ErrorCodes.NotFound, "Order not found.");

                var now = DateTime.UtcNow;
                var shipment = await _db.ZansiDispatchShipments.FirstOrDefaultAsync(s => s.OrderId == request.OrderId, ct);

                // Idempotency FIRST: already booked with a courier → safe no-op
                // return, before any guard, so a duplicate call after a successful
                // booking is harmless (no error, no second charge).
                if (shipment is not null && !string.IsNullOrWhiteSpace(shipment.ProviderShipmentId))
                    return Result<ShipmentDto>.Success(MapShipment(shipment), "Shipment already booked.");

                // ── SAFETY GATE ───────────────────────────────────────────────
                // Never place a billable courier booking before the order is paid
                // + seller-accepted, the option matches the order/customer, and
                // (for a courier booking) booking is enabled and the addresses +
                // parcel are complete. Validation runs BEFORE we create a shipment
                // row or call the provider — a failed check creates nothing and
                // never reaches the courier.
                var shipmentProvider = ResolveShipmentProvider(option.ProviderType);
                var willCallProvider = shipmentProvider is not null && shipmentProvider.IsEnabled
                    && option.ServiceLevel != ZansiDispatchServiceLevel.Collection;

                if (option.ServiceLevel == ZansiDispatchServiceLevel.Collection)
                    return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, "Collection options cannot be booked with courier.");

                if (order.PaymentStatus != ZansiHustle.Shared.Enums.Orders.PaymentStatus.Paid)
                    return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, "Order must be paid before dispatch can be booked.");

                // Seller acceptance moves a product order to Confirmed (see
                // OrderService.AcceptAsync); AwaitingSellerAcceptance/Pending means
                // the seller hasn't accepted yet.
                if (order.Status != ZansiHustle.Shared.Enums.Orders.OrderStatus.Confirmed)
                    return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, "Seller must accept the order before dispatch can be booked.");

                // The option must be the one the customer selected + paid for on THIS order.
                var optionMatchesOrder = order.DeliveryQuoteOptionId is Guid sel && sel == option.Id;
                if (quote.UserId != order.BuyerUserId || !optionMatchesOrder)
                    return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, "This delivery option does not belong to the order/customer.");

                if (willCallProvider)
                {
                    // Kill switch — refuse to call the provider booking endpoint
                    // unless explicitly enabled for this environment.
                    if (!_opts.CourierGuy.AllowShipmentBooking)
                        return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, "Courier booking is disabled in this environment.");

                    // Hard-require complete pickup/delivery contact + address
                    // (incl. postal codes) and valid parcel before any /shipments call.
                    var gate = ValidateCourierBookingReadiness(quote, request);
                    if (gate is not null)
                        return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, gate);
                }

                if (shipment is null)
                {
                    shipment = new ZansiDispatchShipment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        QuoteId = quote.Id,
                        QuoteOptionId = option.Id,
                        UserId = order.BuyerUserId,
                        MerchantId = order.MerchantId,
                        ShopId = quote.ShopId,
                        ProviderType = option.ProviderType,
                        ServiceLevel = option.ServiceLevel,
                        ServiceLevelCode = option.ServiceLevelCode,
                        ServiceLevelName = option.ServiceLevelName,
                        QuotedDeliveryFee = option.QuotedAmount,
                        Status = ZansiDispatchShipmentStatus.PendingDispatch,
                        ReconciliationStatus = ZansiDispatchReconciliationStatus.Pending,
                        PickupAddressSummary = quote.SellerAddressSummary,
                        DropoffAddressSummary = quote.BuyerAddressSummary,
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                    _db.ZansiDispatchShipments.Add(shipment);
                    _db.ZansiDispatchLedgerEntries.Add(QuoteChargedLedger(shipment.Id, order.Id, option.QuotedAmount, order.BuyerUserId, now));
                }

                // Book with Courier Guy only when the option is a courier option,
                // the provider is enabled, AND booking passed the gate above.
                // Otherwise the shipment stays PendingDispatch (no courier booking).
                if (willCallProvider)
                {
                    var providerReq = BuildShipmentRequest(shipment, quote, option, order.Code, request);
                    var sw = Stopwatch.StartNew();
                    Result<ProviderShipmentResult> pr;
                    try { pr = await shipmentProvider.CreateShipmentAsync(providerReq, ct); }
                    catch (Exception ex) { pr = Result<ProviderShipmentResult>.Failure(ErrorCodes.Exception, ex.Message); }
                    sw.Stop();

                    var data = pr.IsSuccess ? pr.Data : null;
                    AddProviderLog(option.ProviderType, ZansiDispatchProviderOperation.CreateShipment,
                        data?.RawRequestJson, data?.RawResponseJson, data?.Ok ?? false,
                        data?.Ok == false ? data.ErrorMessage : (pr.IsSuccess ? null : pr.Message),
                        data?.StatusCode, (int)sw.ElapsedMilliseconds);

                    if (data is null || !data.Ok)
                    {
                        // Do NOT fake a booked shipment — surface the provider error.
                        await _db.SaveChangesAsync(ct);
                        return Result<ShipmentDto>.Failure(ErrorCodes.Exception,
                            data?.ErrorMessage ?? pr.Message ?? "Could not book the shipment with the courier.");
                    }

                    shipment.ProviderShipmentId = data.ProviderShipmentId;
                    shipment.ProviderShipmentReference = data.ProviderShipmentReference;
                    shipment.TrackingNumber = data.TrackingNumber;
                    shipment.ShortTrackingReference = data.ShortTrackingReference;
                    shipment.ServiceLevelCode = data.ServiceLevelCode ?? shipment.ServiceLevelCode;
                    shipment.ServiceLevelName = data.ServiceLevelName ?? shipment.ServiceLevelName;
                    shipment.RawProviderResponseJson = data.RawResponseJson;
                    shipment.Status = data.InitialStatus ?? ZansiDispatchShipmentStatus.BookedWithCourier;
                    shipment.UpdatedAt = now;

                    _db.ZansiDispatchShipmentEvents.Add(new ZansiDispatchShipmentEvent
                    {
                        Id = Guid.NewGuid(),
                        ShipmentId = shipment.Id,
                        ProviderType = option.ProviderType,
                        ProviderStatus = data.InitialProviderStatus ?? "submitted",
                        InternalStatus = shipment.Status,
                        Message = "Shipment booked with courier.",
                        EventTime = now,
                        CreatedAt = now,
                    });
                }

                await _db.SaveChangesAsync(ct);
                return Result<ShipmentDto>.Success(MapShipment(shipment), "Shipment created.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch CreateShipmentFromQuote failed. OrderId={OrderId}", request?.OrderId);
                return Result<ShipmentDto>.Failure(ErrorCodes.Exception, "Could not create the shipment.");
            }
        }

        public async Task<Result<TrackingResultDto>> TrackShipmentAsync(Guid shipmentId, CancellationToken ct = default)
        {
            try
            {
                var s = await _db.ZansiDispatchShipments.FirstOrDefaultAsync(x => x.Id == shipmentId, ct);
                if (s is null) return Result<TrackingResultDto>.Failure(ErrorCodes.NotFound, "Shipment not found.");

                var provider = ResolveShipmentProvider(s.ProviderType);
                var trackingRef = s.TrackingNumber ?? s.ShortTrackingReference;

                if (provider is not null && provider.IsEnabled && !string.IsNullOrWhiteSpace(trackingRef))
                {
                    var sw = Stopwatch.StartNew();
                    Result<ProviderTrackingResult> tr;
                    try { tr = await provider.GetShipmentStatusAsync(trackingRef!, ct); }
                    catch (Exception ex) { tr = Result<ProviderTrackingResult>.Failure(ErrorCodes.Exception, ex.Message); }
                    sw.Stop();

                    var data = tr.IsSuccess ? tr.Data : null;
                    AddProviderLog(s.ProviderType, ZansiDispatchProviderOperation.GetStatus, null, data?.RawResponseJson,
                        data?.Ok ?? false, data?.Ok == false ? data.ErrorMessage : (tr.IsSuccess ? null : tr.Message),
                        data?.StatusCode, (int)sw.ElapsedMilliseconds);

                    if (data is { Ok: true })
                        await PersistTrackingEventsAsync(s, data, ct);
                }

                var events = await _db.ZansiDispatchShipmentEvents.AsNoTracking()
                    .Where(e => e.ShipmentId == s.Id)
                    .OrderBy(e => e.EventTime)
                    .ToListAsync(ct);

                return Result<TrackingResultDto>.Success(new TrackingResultDto
                {
                    ShipmentId = s.Id,
                    Status = s.Status,
                    ReconciliationStatus = s.ReconciliationStatus,
                    TrackingNumber = s.TrackingNumber,
                    DeliveredAt = s.DeliveredAt,
                    Events = events.Select(MapEvent).ToList(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch TrackShipment failed. Id={Id}", shipmentId);
                return Result<TrackingResultDto>.Failure(ErrorCodes.Exception, "Could not track the shipment.");
            }
        }

        public async Task<Result<OrderDispatchSnapshotDto>> GetOrderDispatchSnapshotAsync(Guid orderId, CancellationToken ct = default)
        {
            try
            {
                // STORED state only — no live provider poll (this is a per-open
                // customer read; polling/webhooks keep the stored state fresh).
                var s = await _db.ZansiDispatchShipments.AsNoTracking()
                    .Where(x => x.OrderId == orderId)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (s is null)
                    return Result<OrderDispatchSnapshotDto>.Success(new OrderDispatchSnapshotDto { HasShipment = false });

                var events = await _db.ZansiDispatchShipmentEvents.AsNoTracking()
                    .Where(e => e.ShipmentId == s.Id)
                    .OrderBy(e => e.EventTime)
                    .ToListAsync(ct);

                return Result<OrderDispatchSnapshotDto>.Success(new OrderDispatchSnapshotDto
                {
                    HasShipment = true,
                    Status = s.Status,
                    TrackingProvider = s.ProviderType.ToString(),
                    TrackingNumber = s.TrackingNumber,
                    DeliveredAt = s.DeliveredAt,
                    Events = events.Select(e => new OrderDispatchEventDto
                    {
                        InternalStatus = e.InternalStatus,
                        Message = e.Message,
                        EventTime = e.EventTime,
                    }).ToList(),
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch GetOrderDispatchSnapshot failed. OrderId={OrderId}", orderId);
                return Result<OrderDispatchSnapshotDto>.Failure(ErrorCodes.Exception, "Could not read dispatch status.");
            }
        }

        public async Task<Result<ShipmentDto>> CancelShipmentAsync(Guid adminUserId, Guid shipmentId, CancelShipmentRequestDto? request, CancellationToken ct = default)
        {
            try
            {
                var s = await _db.ZansiDispatchShipments.FirstOrDefaultAsync(x => x.Id == shipmentId, ct);
                if (s is null) return Result<ShipmentDto>.Failure(ErrorCodes.NotFound, "Shipment not found.");

                if (s.Status is ZansiDispatchShipmentStatus.Delivered or ZansiDispatchShipmentStatus.Returned or ZansiDispatchShipmentStatus.Cancelled)
                    return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, $"A {s.Status} shipment cannot be cancelled.");

                var now = DateTime.UtcNow;
                var provider = ResolveShipmentProvider(s.ProviderType);
                var trackingRef = s.TrackingNumber ?? s.ShortTrackingReference;

                if (provider is not null && provider.IsEnabled && !string.IsNullOrWhiteSpace(trackingRef))
                {
                    var sw = Stopwatch.StartNew();
                    Result<ProviderCancelResult> cr;
                    try { cr = await provider.CancelShipmentAsync(trackingRef!, ct); }
                    catch (Exception ex) { cr = Result<ProviderCancelResult>.Failure(ErrorCodes.Exception, ex.Message); }
                    sw.Stop();

                    var data = cr.IsSuccess ? cr.Data : null;
                    AddProviderLog(s.ProviderType, ZansiDispatchProviderOperation.CancelShipment, null, data?.RawResponseJson,
                        data?.Ok ?? false, data?.Ok == false ? data.ErrorMessage : (cr.IsSuccess ? null : cr.Message),
                        data?.StatusCode, (int)sw.ElapsedMilliseconds);

                    if (data is null || !data.Ok)
                    {
                        await _db.SaveChangesAsync(ct);
                        return Result<ShipmentDto>.Failure(ErrorCodes.Exception, data?.ErrorMessage ?? cr.Message ?? "Courier cancellation failed.");
                    }
                }

                s.Status = ZansiDispatchShipmentStatus.Cancelled;
                if (!string.IsNullOrWhiteSpace(request?.Reason)) s.Notes = request!.Reason!.Trim();
                s.UpdatedAt = now;
                _db.ZansiDispatchShipmentEvents.Add(new ZansiDispatchShipmentEvent
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = s.Id,
                    ProviderType = s.ProviderType,
                    ProviderStatus = "cancelled",
                    InternalStatus = ZansiDispatchShipmentStatus.Cancelled,
                    Message = request?.Reason ?? "Shipment cancelled.",
                    EventTime = now,
                    CreatedAt = now,
                });
                await _db.SaveChangesAsync(ct);
                return Result<ShipmentDto>.Success(MapShipment(s), "Shipment cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch CancelShipment failed. Id={Id}", shipmentId);
                return Result<ShipmentDto>.Failure(ErrorCodes.Exception, "Could not cancel the shipment.");
            }
        }

        public async Task<Result<ShipmentLabelDto>> GetShipmentLabelAsync(Guid shipmentId, CancellationToken ct = default)
        {
            try
            {
                var s = await _db.ZansiDispatchShipments.FirstOrDefaultAsync(x => x.Id == shipmentId, ct);
                if (s is null) return Result<ShipmentLabelDto>.Failure(ErrorCodes.NotFound, "Shipment not found.");

                // Re-use a still-valid cached label.
                if (!string.IsNullOrWhiteSpace(s.LabelUrl) && s.LabelUrlExpiresAt is DateTime exp && exp > DateTime.UtcNow.AddMinutes(2))
                    return Result<ShipmentLabelDto>.Success(new ShipmentLabelDto { ShipmentId = s.Id, LabelUrl = s.LabelUrl!, ExpiresAt = s.LabelUrlExpiresAt });

                var provider = ResolveShipmentProvider(s.ProviderType);
                if (provider is null || !provider.IsEnabled || string.IsNullOrWhiteSpace(s.ProviderShipmentId))
                    return Result<ShipmentLabelDto>.Failure(ErrorCodes.BadRequest, "No courier label is available for this shipment.");

                var sw = Stopwatch.StartNew();
                Result<ProviderLabelResult> lr;
                try { lr = await provider.GetShipmentLabelAsync(s.ProviderShipmentId!, ct); }
                catch (Exception ex) { lr = Result<ProviderLabelResult>.Failure(ErrorCodes.Exception, ex.Message); }
                sw.Stop();

                var data = lr.IsSuccess ? lr.Data : null;
                AddProviderLog(s.ProviderType, ZansiDispatchProviderOperation.GetLabel, null, data?.RawResponseJson,
                    data?.Ok ?? false, data?.Ok == false ? data.ErrorMessage : (lr.IsSuccess ? null : lr.Message),
                    data?.StatusCode, (int)sw.ElapsedMilliseconds);

                if (data is null || !data.Ok || string.IsNullOrWhiteSpace(data.LabelUrl))
                {
                    await _db.SaveChangesAsync(ct);
                    return Result<ShipmentLabelDto>.Failure(ErrorCodes.Exception, data?.ErrorMessage ?? lr.Message ?? "Could not fetch the label.");
                }

                s.LabelUrl = data.LabelUrl;
                s.LabelUrlExpiresAt = data.ExpiresAt ?? DateTime.UtcNow.AddHours(24);
                s.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                return Result<ShipmentLabelDto>.Success(new ShipmentLabelDto { ShipmentId = s.Id, LabelUrl = data.LabelUrl!, ExpiresAt = s.LabelUrlExpiresAt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch GetShipmentLabel failed. Id={Id}", shipmentId);
                return Result<ShipmentLabelDto>.Failure(ErrorCodes.Exception, "Could not fetch the label.");
            }
        }

        public async Task<Result<WebhookAckDto>> HandleCourierWebhookAsync(string rawBody, string? authHeader, CancellationToken ct = default)
        {
            // Verify optional shared secret. When configured and mismatched, reject.
            var secret = _opts.CourierGuy.WebhookSecret;
            if (!string.IsNullOrWhiteSpace(secret))
            {
                var provided = (authHeader ?? string.Empty).Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (!string.Equals(provided, secret, StringComparison.Ordinal))
                    return Result<WebhookAckDto>.Failure(ErrorCodes.Unauthorized, "Invalid webhook signature.");
            }

            var ack = new WebhookAckDto { Received = true };
            try
            {
                AddProviderLog(ZansiDispatchProviderType.CourierGuy, ZansiDispatchProviderOperation.Webhook,
                    requestJson: TruncateRaw(rawBody), responseJson: null, ok: true, error: null, statusCode: null, durationMs: null);

                if (string.IsNullOrWhiteSpace(rawBody))
                {
                    await _db.SaveChangesAsync(ct);
                    return Result<WebhookAckDto>.Success(ack);
                }

                using var doc = JsonDocument.Parse(rawBody);
                var root = doc.RootElement;

                var trackingRef = ReadString(root, "tracking_reference", "short_tracking_reference", "custom_tracking_reference");
                var providerShipmentId = ReadString(root, "id", "shipment_id");
                var customerRef = ReadString(root, "customer_reference");

                var shipment = await ResolveWebhookShipmentAsync(trackingRef, providerShipmentId, customerRef, ct);
                if (shipment is not null)
                {
                    ack.ShipmentMatched = true;
                    var now = DateTime.UtcNow;

                    // Single status or an events array — handle both shapes.
                    var single = ReadString(root, "status", "tracking_status");
                    if (!string.IsNullOrWhiteSpace(single))
                    {
                        ack.EventsRecorded += await RecordWebhookEventAsync(shipment, single!,
                            ReadString(root, "message", "description"), ReadString(root, "location"),
                            ReadString(root, "event_id", "id"), now, ct);
                    }
                    foreach (var key in new[] { "events", "tracking_events" })
                    {
                        if (root.TryGetProperty(key, out var arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var ev in arr.EnumerateArray())
                            {
                                var st = ReadString(ev, "status", "tracking_status");
                                if (string.IsNullOrWhiteSpace(st)) continue;
                                ack.EventsRecorded += await RecordWebhookEventAsync(shipment, st!,
                                    ReadString(ev, "message", "description"), ReadString(ev, "location"),
                                    ReadString(ev, "event_id", "id"), now, ct);
                            }
                        }
                    }
                    shipment.UpdatedAt = now;
                }

                await _db.SaveChangesAsync(ct);
                return Result<WebhookAckDto>.Success(ack);
            }
            catch (Exception ex)
            {
                // Never crash on unknown payloads — ack receipt so the courier
                // doesn't retry-storm; the raw event is already logged.
                _logger.LogWarning(ex, "ZansiDispatch webhook processing tolerated an error.");
                try { await _db.SaveChangesAsync(ct); } catch { /* ignore */ }
                return Result<WebhookAckDto>.Success(ack);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Command centre
        // ════════════════════════════════════════════════════════════════════

        public async Task<Result<CommandCentreOverviewDto>> GetOverviewAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
        {
            try
            {
                var now = DateTime.UtcNow;
                var start = from ?? now.AddDays(-30);
                var end = to ?? now;

                var q = _db.ZansiDispatchShipments.AsNoTracking().Where(s => s.CreatedAt >= start && s.CreatedAt < end);
                var logs = _db.ZansiDispatchProviderRequestLogs.AsNoTracking().Where(l => l.CreatedAt >= start && l.CreatedAt < end);

                var dto = new CommandCentreOverviewDto
                {
                    PeriodStart = start,
                    PeriodEnd = end,
                    TotalQuotedDeliveryFees = await q.SumAsync(s => (decimal?)s.QuotedDeliveryFee, ct) ?? 0m,
                    TotalActualCourierCosts = await q.Where(s => s.ActualCourierCost != null).SumAsync(s => (decimal?)s.ActualCourierCost, ct) ?? 0m,
                    TotalSurplus = await q.SumAsync(s => (decimal?)s.SurplusAmount, ct) ?? 0m,
                    TotalDeficit = await q.SumAsync(s => (decimal?)s.DeficitAmount, ct) ?? 0m,
                    ShipmentsPendingDispatch = await q.CountAsync(s => s.Status == ZansiDispatchShipmentStatus.PendingDispatch, ct),
                    ShipmentsBookedWithCourier = await q.CountAsync(s => s.Status == ZansiDispatchShipmentStatus.BookedWithCourier, ct),
                    ShipmentsInTransit = await q.CountAsync(s => s.Status == ZansiDispatchShipmentStatus.InTransit || s.Status == ZansiDispatchShipmentStatus.OutForDelivery, ct),
                    ShipmentsDelivered = await q.CountAsync(s => s.Status == ZansiDispatchShipmentStatus.Delivered, ct),
                    ShipmentsExceptions = await q.CountAsync(s => s.Status == ZansiDispatchShipmentStatus.Failed || s.Status == ZansiDispatchShipmentStatus.Exception, ct),
                    ShipmentsPendingReconciliation = await q.CountAsync(s => s.ReconciliationStatus == ZansiDispatchReconciliationStatus.Pending, ct),
                    ShipmentsTotal = await q.CountAsync(ct),
                    ProviderFailureCount = await logs.CountAsync(l => !l.IsSuccess, ct),
                    FallbackQuoteCount = await logs.CountAsync(l => l.Operation == ZansiDispatchProviderOperation.GetRates
                        && !l.IsSuccess && l.ProviderType != ZansiDispatchProviderType.InternalEstimate, ct),
                };
                dto.NetLogisticsBalance = dto.TotalSurplus - dto.TotalDeficit;
                return Result<CommandCentreOverviewDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch GetOverview failed.");
                return Result<CommandCentreOverviewDto>.Failure(ErrorCodes.Exception, "Could not load the command-centre overview.");
            }
        }

        public async Task<Result<List<ShipmentListItemDto>>> GetShipmentsAsync(ZansiDispatchShipmentStatus? status, int page, int pageSize, CancellationToken ct = default)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);
                var q = _db.ZansiDispatchShipments.AsNoTracking().AsQueryable();
                if (status.HasValue) q = q.Where(s => s.Status == status.Value);
                var rows = await q.OrderByDescending(s => s.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

                // Resolve buyer display names in one batched query (no per-row N+1).
                var names = await ResolveCustomerNamesAsync(rows.Select(r => r.UserId), ct);
                return Result<List<ShipmentListItemDto>>.Success(
                    rows.Select(s => MapShipmentListItem(s, names.GetValueOrDefault(s.UserId))).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch GetShipments failed.");
                return Result<List<ShipmentListItemDto>>.Failure(ErrorCodes.Exception, "Could not load shipments.");
            }
        }

        public async Task<Result<ShipmentDto>> GetShipmentAsync(Guid shipmentId, CancellationToken ct = default)
        {
            try
            {
                var s = await _db.ZansiDispatchShipments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == shipmentId, ct);
                if (s is null) return Result<ShipmentDto>.Failure(ErrorCodes.NotFound, "Shipment not found.");
                var dto = MapShipment(s);
                var names = await ResolveCustomerNamesAsync(new[] { s.UserId }, ct);
                dto.CustomerName = names.GetValueOrDefault(s.UserId);
                return Result<ShipmentDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch GetShipment failed. Id={Id}", shipmentId);
                return Result<ShipmentDto>.Failure(ErrorCodes.Exception, "Could not load the shipment.");
            }
        }

        public async Task<Result<ShipmentDto>> CaptureActualCostAsync(Guid adminUserId, Guid shipmentId, CaptureActualCostRequestDto request, CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.ActualCourierCost < 0m)
                    return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, "A non-negative actual courier cost is required.");

                var s = await _db.ZansiDispatchShipments.FirstOrDefaultAsync(x => x.Id == shipmentId, ct);
                if (s is null) return Result<ShipmentDto>.Failure(ErrorCodes.NotFound, "Shipment not found.");

                var now = DateTime.UtcNow;
                var wasCaptured = s.ActualCourierCost.HasValue;
                var actual = request.ActualCourierCost;

                s.ActualCourierCost = actual;
                var net = s.QuotedDeliveryFee - actual;
                s.NetAmount = net;
                s.SurplusAmount = net > 0m ? net : 0m;
                s.DeficitAmount = net < 0m ? -net : 0m;
                s.ReconciliationStatus = wasCaptured ? ZansiDispatchReconciliationStatus.Adjusted : ZansiDispatchReconciliationStatus.Reconciled;
                if (!string.IsNullOrWhiteSpace(request.CourierReference)) s.CourierReference = request.CourierReference.Trim();
                if (!string.IsNullOrWhiteSpace(request.Notes)) s.Notes = request.Notes.Trim();
                s.UpdatedAt = now;

                _db.ZansiDispatchLedgerEntries.Add(Ledger(s.Id, s.OrderId, ZansiDispatchLedgerEntryType.ActualCourierCost, actual, ZansiDispatchLedgerDirection.Debit, -actual, "Actual courier cost captured.", s.CourierReference, adminUserId, now));
                if (s.SurplusAmount > 0m)
                    _db.ZansiDispatchLedgerEntries.Add(Ledger(s.Id, s.OrderId, ZansiDispatchLedgerEntryType.SurplusRecognised, s.SurplusAmount, ZansiDispatchLedgerDirection.Credit, s.SurplusAmount, "Surplus recognised (quoted fee exceeded actual cost).", null, adminUserId, now));
                else if (s.DeficitAmount > 0m)
                    _db.ZansiDispatchLedgerEntries.Add(Ledger(s.Id, s.OrderId, ZansiDispatchLedgerEntryType.DeficitRecognised, s.DeficitAmount, ZansiDispatchLedgerDirection.Debit, -s.DeficitAmount, "Deficit recognised (actual cost exceeded quoted fee).", null, adminUserId, now));
                if (wasCaptured)
                    _db.ZansiDispatchLedgerEntries.Add(Ledger(s.Id, s.OrderId, ZansiDispatchLedgerEntryType.ManualAdjustment, 0m, ZansiDispatchLedgerDirection.Credit, 0m, "Actual cost re-captured — surplus/deficit/net recomputed.", null, adminUserId, now));

                await _db.SaveChangesAsync(ct);
                return Result<ShipmentDto>.Success(MapShipment(s), "Actual cost captured.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch CaptureActualCost failed. Id={Id}", shipmentId);
                return Result<ShipmentDto>.Failure(ErrorCodes.Exception, "Could not capture the actual cost.");
            }
        }

        public async Task<Result<ShipmentDto>> UpdateShipmentStatusAsync(Guid adminUserId, Guid shipmentId, UpdateShipmentStatusRequestDto request, CancellationToken ct = default)
        {
            try
            {
                if (request is null || !Enum.IsDefined(typeof(ZansiDispatchShipmentStatus), request.Status))
                    return Result<ShipmentDto>.Failure(ErrorCodes.BadRequest, "A valid shipment status is required.");

                var s = await _db.ZansiDispatchShipments.FirstOrDefaultAsync(x => x.Id == shipmentId, ct);
                if (s is null) return Result<ShipmentDto>.Failure(ErrorCodes.NotFound, "Shipment not found.");

                var now = DateTime.UtcNow;
                s.Status = request.Status;
                if (!string.IsNullOrWhiteSpace(request.TrackingNumber)) s.TrackingNumber = request.TrackingNumber.Trim();
                if (!string.IsNullOrWhiteSpace(request.Notes)) s.Notes = request.Notes.Trim();
                if (request.Status == ZansiDispatchShipmentStatus.Delivered) s.DeliveredAt = request.DeliveredAt ?? now;
                s.UpdatedAt = now;

                _db.ZansiDispatchShipmentEvents.Add(new ZansiDispatchShipmentEvent
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = s.Id,
                    ProviderType = s.ProviderType,
                    ProviderStatus = "manual-update",
                    InternalStatus = s.Status,
                    Message = "Status updated by ops.",
                    EventTime = now,
                    CreatedAt = now,
                });

                await _db.SaveChangesAsync(ct);
                return Result<ShipmentDto>.Success(MapShipment(s), "Shipment updated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch UpdateShipmentStatus failed. Id={Id}", shipmentId);
                return Result<ShipmentDto>.Failure(ErrorCodes.Exception, "Could not update the shipment.");
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════

        private IZansiDispatchShipmentProvider? ResolveShipmentProvider(ZansiDispatchProviderType type)
            => _shipmentProviders.FirstOrDefault(p => p.ProviderType == type);

        private async Task<Dictionary<string, string>> LoadSettingsAsync(CancellationToken ct)
            => await _db.ZansiDispatchSettings.AsNoTracking().Where(s => s.IsActive).ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        private void AddProviderLog(ZansiDispatchProviderType providerType, ZansiDispatchProviderOperation op,
            string? requestJson, string? responseJson, bool ok, string? error, int? statusCode, int? durationMs)
        {
            try
            {
                _db.ZansiDispatchProviderRequestLogs.Add(new ZansiDispatchProviderRequestLog
                {
                    Id = Guid.NewGuid(),
                    ProviderType = providerType,
                    Operation = op,
                    RequestJson = requestJson,
                    ResponseJson = responseJson,
                    IsSuccess = ok,
                    ErrorMessage = error,
                    StatusCode = statusCode,
                    DurationMs = durationMs,
                    CreatedAt = DateTime.UtcNow,
                });
            }
            catch (Exception ex) { _logger.LogWarning(ex, "ZansiDispatch: provider-request-log write skipped."); }
        }

        private ProviderShipmentRequest BuildShipmentRequest(ZansiDispatchShipment shipment, ZansiDispatchQuote quote, ZansiDispatchQuoteOption option, string? orderCode, CreateShipmentFromQuoteRequestDto req)
            => new()
            {
                ShipmentId = shipment.Id,
                OrderId = shipment.OrderId,
                CollectionAddress = new ProviderAddress
                {
                    Type = quote.SellerAddressType ?? ZansiDispatchAddressType.Business,
                    StreetAddress = quote.SellerStreetAddress,
                    LocalArea = quote.SellerLocalArea,
                    City = quote.SellerCity,
                    Zone = quote.SellerProvince,
                    Country = quote.SellerCountry ?? "ZA",
                    Code = quote.SellerPostalCode,
                    Lat = quote.SellerLat,
                    Lng = quote.SellerLng,
                },
                CollectionContact = new ProviderContact { Name = req.CollectionContact?.Name, MobileNumber = req.CollectionContact?.MobileNumber, Email = req.CollectionContact?.Email },
                DeliveryAddress = new ProviderAddress
                {
                    Type = quote.BuyerAddressType ?? ZansiDispatchAddressType.Residential,
                    StreetAddress = quote.BuyerStreetAddress,
                    LocalArea = quote.BuyerLocalArea,
                    City = quote.BuyerCity,
                    Zone = quote.BuyerProvince,
                    Country = quote.BuyerCountry ?? "ZA",
                    Code = quote.BuyerPostalCode,
                    Lat = quote.BuyerLat,
                    Lng = quote.BuyerLng,
                },
                DeliveryContact = new ProviderContact { Name = req.DeliveryContact?.Name, MobileNumber = req.DeliveryContact?.MobileNumber, Email = req.DeliveryContact?.Email },
                Parcels = new List<ProviderParcel>
                {
                    new()
                    {
                        Description = quote.ParcelDescription,
                        SubmittedLengthCm = quote.SubmittedLengthCm,
                        SubmittedWidthCm = quote.SubmittedWidthCm,
                        SubmittedHeightCm = quote.SubmittedHeightCm,
                        SubmittedWeightKg = quote.EstimatedWeightKg,
                    },
                },
                DeclaredValue = quote.DeclaredValue,
                CustomerReference = req.CustomerReference ?? orderCode,
                CustomerReferenceName = string.IsNullOrWhiteSpace(req.CustomerReferenceName) ? "Order no." : req.CustomerReferenceName,
                SpecialInstructionsCollection = req.SpecialInstructionsCollection,
                SpecialInstructionsDelivery = req.SpecialInstructionsDelivery,
                MuteNotifications = req.MuteNotifications,
                ServiceLevelCode = option.ServiceLevelCode,
                ServiceLevelId = option.ProviderServiceLevelId,
            };

        private async Task PersistTrackingEventsAsync(ZansiDispatchShipment s, ProviderTrackingResult data, CancellationToken ct)
        {
            var existing = await _db.ZansiDispatchShipmentEvents.Where(e => e.ShipmentId == s.Id).ToListAsync(ct);
            var now = DateTime.UtcNow;
            foreach (var ev in data.Events)
            {
                var dup = existing.Any(e =>
                    (!string.IsNullOrWhiteSpace(ev.ProviderEventId) && e.ProviderEventId == ev.ProviderEventId)
                    || (e.ProviderStatus == ev.ProviderStatus && e.EventTime == ev.EventTime));
                if (dup) continue;
                var entity = new ZansiDispatchShipmentEvent
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = s.Id,
                    ProviderType = s.ProviderType,
                    ProviderEventId = ev.ProviderEventId,
                    ProviderStatus = ev.ProviderStatus,
                    InternalStatus = ev.InternalStatus,
                    Message = ev.Message,
                    Location = ev.Location,
                    EventTime = ev.EventTime,
                    RawEventJson = ev.RawJson,
                    CreatedAt = now,
                };
                _db.ZansiDispatchShipmentEvents.Add(entity);
                existing.Add(entity);
            }
            if (data.CurrentStatus is ZansiDispatchShipmentStatus cur)
            {
                s.Status = cur;
                if (cur == ZansiDispatchShipmentStatus.Delivered && s.DeliveredAt is null) s.DeliveredAt = now;
                s.UpdatedAt = now;
            }
        }

        private async Task<ZansiDispatchShipment?> ResolveWebhookShipmentAsync(string? trackingRef, string? providerShipmentId, string? customerRef, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(trackingRef))
            {
                var hit = await _db.ZansiDispatchShipments.FirstOrDefaultAsync(s => s.TrackingNumber == trackingRef || s.ShortTrackingReference == trackingRef, ct);
                if (hit is not null) return hit;
            }
            if (!string.IsNullOrWhiteSpace(providerShipmentId))
            {
                var hit = await _db.ZansiDispatchShipments.FirstOrDefaultAsync(s => s.ProviderShipmentId == providerShipmentId, ct);
                if (hit is not null) return hit;
            }
            if (!string.IsNullOrWhiteSpace(customerRef))
            {
                var orderId = await _db.Orders.AsNoTracking().Where(o => o.Code == customerRef).Select(o => (Guid?)o.Id).FirstOrDefaultAsync(ct);
                if (orderId is Guid oid)
                    return await _db.ZansiDispatchShipments.FirstOrDefaultAsync(s => s.OrderId == oid, ct);
            }
            return null;
        }

        private async Task<int> RecordWebhookEventAsync(ZansiDispatchShipment s, string providerStatus, string? message, string? location, string? eventId, DateTime now, CancellationToken ct)
        {
            var internalStatus = ZansiDispatch.Providers.CourierGuy.CourierGuyStatusMap.Map(providerStatus);
            if (!string.IsNullOrWhiteSpace(eventId) && await _db.ZansiDispatchShipmentEvents.AnyAsync(e => e.ShipmentId == s.Id && e.ProviderEventId == eventId, ct))
                return 0;

            _db.ZansiDispatchShipmentEvents.Add(new ZansiDispatchShipmentEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = s.Id,
                ProviderType = s.ProviderType,
                ProviderEventId = eventId,
                ProviderStatus = providerStatus,
                InternalStatus = internalStatus,
                Message = message,
                Location = location,
                EventTime = now,
                CreatedAt = now,
            });
            s.Status = internalStatus;
            if (internalStatus == ZansiDispatchShipmentStatus.Delivered && s.DeliveredAt is null) s.DeliveredAt = now;
            return 1;
        }

        private static ZansiDispatchLedgerEntry QuoteChargedLedger(Guid shipmentId, Guid orderId, decimal fee, Guid userId, DateTime now)
            => Ledger(shipmentId, orderId, ZansiDispatchLedgerEntryType.QuoteCharged, fee, ZansiDispatchLedgerDirection.Credit, fee, "Delivery fee charged to buyer at checkout.", null, userId, now);

        private static ZansiDispatchLedgerEntry Ledger(Guid shipmentId, Guid? orderId, ZansiDispatchLedgerEntryType type, decimal amount, ZansiDispatchLedgerDirection dir, decimal impact, string desc, string? reference, Guid? userId, DateTime now)
            => new()
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                OrderId = orderId,
                EntryType = type,
                Amount = amount,
                Direction = dir,
                BalanceImpact = impact,
                Description = desc,
                Reference = reference,
                CreatedByUserId = userId,
                CreatedAt = now,
            };

        private static ZansiDispatchQuoteOption MapProviderOption(Guid quoteId, ProviderQuoteOption po, DateTime now) => new()
        {
            Id = Guid.NewGuid(),
            QuoteId = quoteId,
            ProviderType = po.ProviderType,
            ProviderQuoteReference = po.ProviderQuoteReference,
            ProviderServiceLevelId = po.ProviderServiceLevelId,
            ServiceLevelCode = po.ServiceLevelCode,
            ServiceLevelName = po.ServiceLevelName,
            ServiceLevel = po.ServiceLevel,
            Label = po.Label,
            Description = po.Description,
            QuotedAmount = po.QuotedAmount,
            VatAmount = po.VatAmount,
            TotalAmount = po.TotalAmount,
            Currency = string.IsNullOrWhiteSpace(po.Currency) ? "ZAR" : po.Currency,
            EstimatedDeliveryDaysMin = po.EstimatedDeliveryDaysMin,
            EstimatedDeliveryDaysMax = po.EstimatedDeliveryDaysMax,
            EstimateBreakdownJson = po.EstimateBreakdownJson,
            RawProviderResponseJson = po.RawProviderResponseJson,
            CreatedAt = now,
            UpdatedAt = now,
        };

        private static QuoteDto MapQuote(ZansiDispatchQuote q) => new()
        {
            QuoteId = q.Id,
            ExpiresAt = q.ExpiresAt,
            Status = q.Status,
            Options = q.Options
                .OrderBy(o => o.ServiceLevel == ZansiDispatchServiceLevel.Collection ? 1 : 0)
                .ThenBy(o => o.QuotedAmount)
                .Select(o => new QuoteOptionDto
                {
                    QuoteOptionId = o.Id,
                    ProviderType = o.ProviderType,
                    ProviderQuoteReference = o.ProviderQuoteReference,
                    ProviderServiceLevelId = o.ProviderServiceLevelId,
                    ServiceLevelCode = o.ServiceLevelCode,
                    ServiceLevelName = o.ServiceLevelName,
                    ServiceLevel = o.ServiceLevel,
                    Label = o.Label,
                    Description = o.Description,
                    QuotedAmount = o.QuotedAmount,
                    VatAmount = o.VatAmount,
                    TotalAmount = o.TotalAmount,
                    Currency = o.Currency,
                    EstimatedDeliveryDaysMin = o.EstimatedDeliveryDaysMin,
                    EstimatedDeliveryDaysMax = o.EstimatedDeliveryDaysMax,
                    EstimateBreakdown = o.EstimateBreakdownJson,
                    IsSelected = o.IsSelected,
                }).ToList(),
        };

        private static ShipmentDto MapShipment(ZansiDispatchShipment s) => new()
        {
            Id = s.Id,
            OrderId = s.OrderId,
            QuoteId = s.QuoteId,
            QuoteOptionId = s.QuoteOptionId,
            UserId = s.UserId,
            MerchantId = s.MerchantId,
            ShopId = s.ShopId,
            ProviderType = s.ProviderType,
            ServiceLevel = s.ServiceLevel,
            ServiceLevelCode = s.ServiceLevelCode,
            ServiceLevelName = s.ServiceLevelName,
            QuotedDeliveryFee = s.QuotedDeliveryFee,
            ActualCourierCost = s.ActualCourierCost,
            SurplusAmount = s.SurplusAmount,
            DeficitAmount = s.DeficitAmount,
            NetAmount = s.NetAmount,
            Status = s.Status,
            ReconciliationStatus = s.ReconciliationStatus,
            ProviderShipmentId = s.ProviderShipmentId,
            TrackingNumber = s.TrackingNumber,
            ShortTrackingReference = s.ShortTrackingReference,
            ProviderShipmentReference = s.ProviderShipmentReference,
            CourierReference = s.CourierReference,
            PickupAddressSummary = s.PickupAddressSummary,
            DropoffAddressSummary = s.DropoffAddressSummary,
            PickupScheduledAt = s.PickupScheduledAt,
            DeliveredAt = s.DeliveredAt,
            LabelUrl = s.LabelUrl,
            LabelUrlExpiresAt = s.LabelUrlExpiresAt,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
        };

        private static ShipmentListItemDto MapShipmentListItem(ZansiDispatchShipment s, string? customerName) => new()
        {
            Id = s.Id,
            OrderId = s.OrderId,
            UserId = s.UserId,
            CustomerName = customerName,
            ProviderType = s.ProviderType,
            ServiceLevel = s.ServiceLevel,
            Status = s.Status,
            ReconciliationStatus = s.ReconciliationStatus,
            QuotedDeliveryFee = s.QuotedDeliveryFee,
            ActualCourierCost = s.ActualCourierCost,
            SurplusAmount = s.SurplusAmount,
            DeficitAmount = s.DeficitAmount,
            NetAmount = s.NetAmount,
            TrackingNumber = s.TrackingNumber,
            ShortTrackingReference = s.ShortTrackingReference,
            CreatedAt = s.CreatedAt,
        };

        /// <summary>
        /// Batch-resolve buyer display names for a set of user ids → { id : name }.
        /// Full name (FirstName LastName), falling back to email. Unresolvable ids
        /// are simply absent from the dictionary. One query, no N+1.
        /// </summary>
        private async Task<Dictionary<Guid, string>> ResolveCustomerNamesAsync(IEnumerable<Guid> userIds, CancellationToken ct)
        {
            var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0) return new Dictionary<Guid, string>();

            var users = await _db.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
                .ToListAsync(ct);

            return users.ToDictionary(
                u => u.Id,
                u =>
                {
                    var full = $"{u.FirstName} {u.LastName}".Trim();
                    return string.IsNullOrWhiteSpace(full) ? (u.Email ?? "") : full;
                });
        }

        private static ShipmentEventDto MapEvent(ZansiDispatchShipmentEvent e) => new()
        {
            ProviderStatus = e.ProviderStatus,
            InternalStatus = e.InternalStatus,
            Message = e.Message,
            Location = e.Location,
            EventTime = e.EventTime,
        };

        private static string? ReadString(JsonElement el, params string[] names)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            foreach (var n in names)
                if (el.TryGetProperty(n, out var p))
                {
                    if (p.ValueKind == JsonValueKind.String) return p.GetString();
                    if (p.ValueKind == JsonValueKind.Number) return p.ToString();
                }
            return null;
        }

        private static string? Trim(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        // ── create-from-quote courier booking gate ───────────────────────────
        // Returns an error message when the order/quote isn't ready for a REAL
        // courier booking, or null when it's safe to call POST /shipments. Only
        // invoked when a courier booking would actually be placed.
        private static string? ValidateCourierBookingReadiness(
            ZansiDispatchQuote quote, CreateShipmentFromQuoteRequestDto request)
        {
            // Pickup (seller/collection) contact + address.
            if (!HasContact(request.CollectionContact))
                return "Pickup contact (name and phone or email) is required before booking courier.";
            if (string.IsNullOrWhiteSpace(quote.SellerStreetAddress) && string.IsNullOrWhiteSpace(quote.SellerAddressSummary))
                return "Pickup street address is required before booking courier.";
            if (string.IsNullOrWhiteSpace(quote.SellerCity))
                return "Pickup city is required before booking courier.";
            if (string.IsNullOrWhiteSpace(quote.SellerProvince))
                return "Pickup province is required before booking courier.";
            if (string.IsNullOrWhiteSpace(quote.SellerPostalCode))
                return "Pickup postal code is required before booking courier.";

            // Delivery (buyer) contact + address.
            if (!HasContact(request.DeliveryContact))
                return "Delivery contact (name and phone or email) is required before booking courier.";
            if (string.IsNullOrWhiteSpace(quote.BuyerStreetAddress) && string.IsNullOrWhiteSpace(quote.BuyerAddressSummary))
                return "Delivery street address is required before booking courier.";
            if (string.IsNullOrWhiteSpace(quote.BuyerCity))
                return "Delivery city is required before booking courier.";
            if (string.IsNullOrWhiteSpace(quote.BuyerProvince))
                return "Delivery province is required before booking courier.";
            if (string.IsNullOrWhiteSpace(quote.BuyerPostalCode))
                return "Delivery postal code is required before booking courier.";

            // Parcel — weight + all three dimensions must be positive; declared value non-negative.
            if (!(quote.EstimatedWeightKg > 0m)
                || !(quote.SubmittedLengthCm > 0m)
                || !(quote.SubmittedWidthCm > 0m)
                || !(quote.SubmittedHeightCm > 0m))
                return "Parcel weight and dimensions are required before booking courier.";
            if (quote.DeclaredValue is decimal dv && dv < 0m)
                return "Declared value cannot be negative.";

            return null;
        }

        private static bool HasContact(DispatchContactDto? c) =>
            c is not null
            && !string.IsNullOrWhiteSpace(c.Name)
            && (!string.IsNullOrWhiteSpace(c.MobileNumber) || !string.IsNullOrWhiteSpace(c.Email));

        private static string? TruncateRaw(string? s) => s is null ? null : (s.Length <= 8000 ? s : s.Substring(0, 8000));
        private static string? SafeJson(object? value) { try { return value is null ? null : JsonSerializer.Serialize(value); } catch { return null; } }

        // Masks the value of any sensitive-looking JSON field before it reaches a
        // log line — a defensive net for the UAT quote debug logs (API keys live
        // in HTTP headers and are never serialized into these payloads, but we
        // never want a key/token/secret/account number to slip through). Also
        // truncates so a huge provider body can't flood the log.
        private static readonly System.Text.RegularExpressions.Regex SensitiveJsonRegex =
            new("\"(apikey|api_key|authorization|password|secret|token|bearer|accountnumber|account_number|webhooksecret)\"\\s*:\\s*\"[^\"]*\"",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        private static string? Redact(string? json)
        {
            if (string.IsNullOrEmpty(json)) return json;
            var masked = SensitiveJsonRegex.Replace(json, m => $"\"{m.Groups[1].Value}\":\"***\"");
            return TruncateRaw(masked);
        }
    }
}
