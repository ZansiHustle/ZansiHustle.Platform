using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.ZansiDispatch;
using ZansiHustle.Application.ZansiDispatch.Providers;
using ZansiHustle.Shared.Enums.ZansiDispatch;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.ZansiDispatch.Providers
{
    /// <summary>
    /// Safety-net provider. Returns a single flat "Standard Delivery" estimate
    /// when the configured default provider can't quote (e.g. a future courier
    /// API is down). Always succeeds so checkout can always present at least one
    /// payable delivery option. Fee = clamp(BaseFee + RiskBuffer, Min, Max).
    /// </summary>
    public sealed class ManualFallbackProvider : IZansiDispatchQuoteProvider
    {
        public ZansiDispatchProviderType ProviderType => ZansiDispatchProviderType.ManualFallback;

        public bool IsEnabled => true;

        public Task<Result<ProviderQuoteResult>> GetQuoteOptionsAsync(
            ZansiDispatchQuoteContext ctx, ZansiDispatchSettings s, CancellationToken ct = default)
        {
            var amount = Math.Clamp(s.BaseFee + s.RiskBuffer, s.MinimumFee, s.MaximumNormalFee);

            var breakdown = JsonSerializer.Serialize(new
            {
                strategy = "ManualFallback",
                baseFee = s.BaseFee,
                riskBuffer = s.RiskBuffer,
                minimumFee = s.MinimumFee,
                maximumNormalFee = s.MaximumNormalFee,
                finalAmount = amount,
            });

            var option = new ProviderQuoteOption
            {
                ProviderType = ZansiDispatchProviderType.ManualFallback,
                ServiceLevel = ZansiDispatchServiceLevel.Standard,
                Label = "Standard Delivery",
                Description = "Delivery handled by ZansiHustle Dispatch (estimated; final cost confirmed by our team)",
                QuotedAmount = amount,
                Currency = "ZAR",
                EstimatedDeliveryDaysMin = 2,
                EstimatedDeliveryDaysMax = 4,
                EstimateBreakdownJson = breakdown,
            };

            return Task.FromResult(Result<ProviderQuoteResult>.Success(new ProviderQuoteResult
            {
                Options = new List<ProviderQuoteOption> { option },
            }));
        }
    }
}
