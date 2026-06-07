using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Application.ZansiDispatch.Providers;
using ZansiHustle.Shared.Enums.ZansiDispatch;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.ZansiDispatch.Providers
{
    /// <summary>
    /// Phase 1 default provider. Produces a single deterministic "Standard
    /// Delivery" option from ZansiDispatch settings + the buyer/seller location
    /// and parcel size. No external calls — pure, reproducible arithmetic.
    ///
    /// Fee = BaseFee + LocationFee + SizeFee + RiskBuffer, clamped to
    /// [MinimumFee, MaximumNormalFee]. Location tier:
    ///   • different province              → DifferentProvinceFee
    ///   • same province, different city   → DifferentCityFee
    ///   • same city                       → SameCityFee
    ///   • seller location unknown         → DifferentCityFee (neutral default)
    /// </summary>
    public sealed class InternalEstimateProvider : IZansiDispatchQuoteProvider
    {
        private readonly ILogger<InternalEstimateProvider> _logger;

        public InternalEstimateProvider(ILogger<InternalEstimateProvider> logger)
        {
            _logger = logger;
        }

        public ZansiDispatchProviderType ProviderType => ZansiDispatchProviderType.InternalEstimate;

        // Deterministic + always available — no credentials, never fails to be enabled.
        public bool IsEnabled => true;

        public Task<Result<ProviderQuoteResult>> GetQuoteOptionsAsync(
            ZansiDispatchQuoteContext ctx, ZansiDispatchSettings s, CancellationToken ct = default)
        {
            try
            {
                var (locationFee, tier, daysMin, daysMax) = ResolveLocation(ctx, s);
                var size = ctx.ItemSizeCategory ?? ZansiDispatchItemSizeCategory.Medium;
                var sizeFee = s.SizeFee(size);

                var raw = s.BaseFee + locationFee + sizeFee + s.RiskBuffer;
                var amount = Math.Clamp(raw, s.MinimumFee, s.MaximumNormalFee);

                var breakdown = JsonSerializer.Serialize(new
                {
                    baseFee = s.BaseFee,
                    locationTier = tier,
                    locationFee,
                    sizeCategory = size.ToString(),
                    sizeFee,
                    riskBuffer = s.RiskBuffer,
                    rawTotal = raw,
                    minimumFee = s.MinimumFee,
                    maximumNormalFee = s.MaximumNormalFee,
                    finalAmount = amount,
                });

                var option = new ProviderQuoteOption
                {
                    ProviderType = ZansiDispatchProviderType.InternalEstimate,
                    ServiceLevel = ZansiDispatchServiceLevel.Standard,
                    Label = "Standard Delivery",
                    Description = "Delivery handled by ZansiHustle Dispatch",
                    QuotedAmount = amount,
                    Currency = "ZAR",
                    EstimatedDeliveryDaysMin = daysMin,
                    EstimatedDeliveryDaysMax = daysMax,
                    EstimateBreakdownJson = breakdown,
                };

                return Task.FromResult(Result<ProviderQuoteResult>.Success(new ProviderQuoteResult
                {
                    Options = new List<ProviderQuoteOption> { option },
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ZansiDispatch InternalEstimate quote failed.");
                return Task.FromResult(Result<ProviderQuoteResult>.Failure(
                    ErrorCodes.Exception, "Could not compute a delivery estimate."));
            }
        }

        private static (decimal fee, string tier, int daysMin, int daysMax) ResolveLocation(
            ZansiDispatchQuoteContext ctx, ZansiDispatchSettings s)
        {
            var sellerProvince = ctx.SellerProvince?.Trim();
            var sellerCity = ctx.SellerCity?.Trim();
            var buyerProvince = ctx.BuyerProvince?.Trim();
            var buyerCity = ctx.BuyerCity?.Trim();

            // Seller location unknown → neutral different-city tier.
            if (string.IsNullOrWhiteSpace(sellerProvince) && string.IsNullOrWhiteSpace(sellerCity))
                return (s.DifferentCityFee, "Unknown", 2, 4);

            if (!string.IsNullOrWhiteSpace(sellerProvince) && !string.IsNullOrWhiteSpace(buyerProvince)
                && !sellerProvince.Equals(buyerProvince, StringComparison.OrdinalIgnoreCase))
                return (s.DifferentProvinceFee, "DifferentProvince", 3, 5);

            if (!string.IsNullOrWhiteSpace(sellerCity) && !string.IsNullOrWhiteSpace(buyerCity)
                && !sellerCity.Equals(buyerCity, StringComparison.OrdinalIgnoreCase))
                return (s.DifferentCityFee, "DifferentCity", 2, 3);

            return (s.SameCityFee, "SameCity", 1, 2);
        }
    }
}
