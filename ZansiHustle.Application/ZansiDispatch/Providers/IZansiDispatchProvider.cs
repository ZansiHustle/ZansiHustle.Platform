using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Shared.Enums.ZansiDispatch;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.ZansiDispatch.Providers
{
    /// <summary>
    /// Base marker for any ZansiDispatch logistics provider. Every provider
    /// declares which <see cref="ZansiDispatchProviderType"/> it represents so
    /// the service can resolve the configured one. Providers may also report
    /// whether they are currently usable (enabled + credentials present).
    /// </summary>
    public interface IZansiDispatchProvider
    {
        ZansiDispatchProviderType ProviderType { get; }

        /// <summary>True when the provider is enabled and has the credentials to operate.</summary>
        bool IsEnabled { get; }
    }

    /// <summary>
    /// A provider that can produce delivery rate options for a quote. The
    /// service code is provider-agnostic — it only sees
    /// <see cref="ProviderQuoteResult"/>, never courier-specific payloads.
    /// </summary>
    public interface IZansiDispatchQuoteProvider : IZansiDispatchProvider
    {
        Task<Result<ProviderQuoteResult>> GetQuoteOptionsAsync(
            ZansiDispatchQuoteContext context,
            ZansiDispatchSettings settings,
            CancellationToken ct = default);
    }

    /// <summary>
    /// A provider that can book / track / cancel / label a real shipment behind
    /// the abstraction. Application/order/mobile code never sees the provider's
    /// payloads — only the mapped contract types here.
    /// </summary>
    public interface IZansiDispatchShipmentProvider : IZansiDispatchProvider
    {
        Task<Result<ProviderShipmentResult>> CreateShipmentAsync(ProviderShipmentRequest request, CancellationToken ct = default);
        Task<Result<ProviderTrackingResult>> GetShipmentStatusAsync(string trackingReference, CancellationToken ct = default);
        Task<Result<ProviderCancelResult>> CancelShipmentAsync(string trackingReference, CancellationToken ct = default);
        Task<Result<ProviderLabelResult>> GetShipmentLabelAsync(string providerShipmentId, CancellationToken ct = default);
    }
}
