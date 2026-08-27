using System.Threading;
using System.Threading.Tasks;

namespace ZansiHustle.Application.Payments.External
{
    /// <summary>
    /// Delivers a single signed callback POST to an external shop's
    /// CallbackUrl. Never throws — failures are reported via the tuple so
    /// callers can persist delivery state without a try/catch at every call site.
    /// </summary>
    public interface IExternalShopCallbackSender
    {
        Task<(bool Success, string? Error)> SendAsync(
            string callbackUrl,
            byte[] bodyBytes,
            string signatureHex,
            CancellationToken cancellationToken = default);
    }
}
