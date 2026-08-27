using Microsoft.Extensions.Options;
using ZansiHustle.Application.Payments.External;

namespace ZansiHustle.API.Services;

/// <summary>
/// Boot-time diagnostic for the External Shop Payments feature. Mirrors
/// <see cref="OzowConfigReporter"/>'s shape: logs once at startup, never a
/// secret value, and hard-fails only for a genuinely unsafe combination.
/// </summary>
public sealed class ExternalPaymentsConfigReporter : IHostedService
{
    private readonly ExternalPaymentsSettings _settings;
    private readonly ExternalShopsOptions _shops;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ExternalPaymentsConfigReporter> _logger;

    public ExternalPaymentsConfigReporter(
        IOptions<ExternalPaymentsSettings> settings,
        IOptions<ExternalShopsOptions> shops,
        IHostEnvironment env,
        ILogger<ExternalPaymentsConfigReporter> logger)
    {
        _settings = settings.Value ?? new ExternalPaymentsSettings();
        _shops = shops.Value ?? new ExternalShopsOptions();
        _env = env;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var enabledShops = _shops.Where(kvp => kvp.Value.Enabled).ToList();

        if (enabledShops.Count == 0)
        {
            _logger.LogInformation("[ExternalPaymentsConfig] No external shops enabled — feature is dormant.");
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiBaseUrl))
        {
            _logger.LogError(
                "[ExternalPaymentsConfig] {Count} shop(s) enabled but ExternalPayments:ApiBaseUrl is empty — " +
                "session creation will fail with PROVIDER_NOT_CONFIGURED until it is set (this ZansiHustle API's own public base URL).",
                enabledShops.Count);
        }
        else if (_settings.ApiBaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                 || _settings.ApiBaseUrl.Contains("127.0.0.1", StringComparison.Ordinal))
        {
            _logger.LogError(
                "[ExternalPaymentsConfig] ExternalPayments:ApiBaseUrl ({Url}) looks like localhost — " +
                "Ozow cannot reach it and callbacks will never resolve their own Ozow return/notify URLs correctly.",
                _settings.ApiBaseUrl);
        }

        foreach (var (shopCode, shop) in enabledShops)
        {
            if (string.IsNullOrWhiteSpace(shop.SharedSecret))
            {
                _logger.LogError(
                    "[ExternalPaymentsConfig] Shop '{ShopCode}' is Enabled but has no SharedSecret configured. " +
                    "Set ExternalShops__{ShopCode}__SharedSecret before this shop can authenticate.",
                    shopCode, shopCode);
            }

            if (shop.AllowedReturnHosts.Count == 0)
            {
                _logger.LogWarning(
                    "[ExternalPaymentsConfig] Shop '{ShopCode}' has an empty AllowedReturnHosts list — every returnUrl will be rejected.",
                    shopCode);
            }

            if (shop.AllowedCallbackHosts.Count == 0)
            {
                _logger.LogWarning(
                    "[ExternalPaymentsConfig] Shop '{ShopCode}' has an empty AllowedCallbackHosts list — every callbackUrl will be rejected.",
                    shopCode);
            }

            _logger.LogInformation(
                "[ExternalPaymentsConfig] Shop '{ShopCode}' ({ShopName}) enabled. AllowedReturnHosts=[{ReturnHosts}] AllowedCallbackHosts=[{CallbackHosts}].",
                shopCode, shop.ShopName, string.Join(", ", shop.AllowedReturnHosts), string.Join(", ", shop.AllowedCallbackHosts));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
