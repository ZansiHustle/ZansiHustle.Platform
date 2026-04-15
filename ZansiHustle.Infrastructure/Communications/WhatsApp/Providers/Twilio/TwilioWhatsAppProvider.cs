using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using ZansiHustle.Application.Communications.WhatsApp;
using ZansiHustle.Application.Communications.WhatsApp.Interfaces;
using ZansiHustle.Application.Communications.WhatsApp.Models;
using ZansiHustle.Infrastructure.Communications.Twilio;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Communications.WhatsApp.Providers.Twilio;

/// <summary>
/// Twilio implementation of <see cref="IWhatsAppProvider"/> using the Programmable
/// Messaging + Content APIs.
/// </summary>
public sealed class TwilioWhatsAppProvider : IWhatsAppProvider
{
    private const string WhatsAppPrefix = "whatsapp:";

    private readonly ITwilioClientProvider _clientProvider;
    private readonly TwilioSettings _settings;
    private readonly ILogger<TwilioWhatsAppProvider> _logger;

    public TwilioWhatsAppProvider(ITwilioClientProvider clientProvider, IOptions<TwilioSettings> options, ILogger<TwilioWhatsAppProvider> logger)
    {
        _clientProvider = clientProvider;
        _settings = options.Value ?? new TwilioSettings();
        _logger = logger;
    }

    public async Task<Result<string>> SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.ToPhoneNumber))
            return Result<string>.Failure(ErrorCodes.BadRequest, "Recipient phone number is required.");
        
        if (string.IsNullOrWhiteSpace(message.Body) && string.IsNullOrWhiteSpace(message.ContentSid))
            return Result<string>.Failure(ErrorCodes.BadRequest, "Either Body or ContentSid is required.");

        if (!_clientProvider.IsConfigured)
            return Result<string>.Failure(ErrorCodes.ProviderNotConfigured, "Twilio is not configured. Set Twilio:AccountSid and Twilio:AuthToken.");
        
        if (!_settings.WhatsApp.HasSender())
            return Result<string>.Failure(ErrorCodes.ProviderNotConfigured, "Twilio WhatsApp sender is not configured.");

        var client = _clientProvider.GetClient();
        if (client is null)
            return Result<string>.Failure(ErrorCodes.ProviderNotConfigured, "Twilio client could not be initialised.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var toPhoneNumber = NormaliseWhatsAppAddress(message.ToPhoneNumber);

            var options = new CreateMessageOptions(new PhoneNumber(toPhoneNumber));

            if (!string.IsNullOrWhiteSpace(_settings.WhatsApp.MessagingServiceSid))
            {
                options.MessagingServiceSid = _settings.WhatsApp.MessagingServiceSid;
            }
            else
            {
                options.From = new PhoneNumber(NormaliseWhatsAppAddress(_settings.WhatsApp.FromPhoneNumber));
            }

            if (!string.IsNullOrWhiteSpace(message.ContentSid))
            {
                options.ContentSid = message.ContentSid;
                if (!string.IsNullOrWhiteSpace(message.ContentVariables))
                {
                    options.ContentVariables = message.ContentVariables;
                }
            }
            else
            {
                options.Body = message.Body;
            }

            var resource = await MessageResource.CreateAsync(options, client);

            _logger.LogInformation(
                "Twilio WhatsApp queued. Sid={Sid}, To={To}, Status={Status}.",
                resource.Sid, toPhoneNumber, resource.Status);

            if (resource.ErrorCode is not null)
            {
                _logger.LogWarning(
                    "Twilio returned an error for WhatsApp {Sid}: {Code} {Message}.",
                    resource.Sid, resource.ErrorCode, resource.ErrorMessage);
                return Result<string>.Failure(ErrorCodes.WhatsAppSendFailed, resource.ErrorMessage ?? "Twilio rejected the WhatsApp message.");
            }

            return Result<string>.Success(resource.Sid, "WhatsApp message queued for delivery.");
        }
        catch (global::Twilio.Exceptions.ApiException ex)
        {
            _logger.LogError(ex, "Twilio API exception while sending WhatsApp to {Phone}. Code={Code}.", message.ToPhoneNumber, ex.Code);
            return Result<string>.Failure(ErrorCodes.WhatsAppSendFailed, $"Twilio error {ex.Code}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected WhatsApp send failure for {Phone}.", message.ToPhoneNumber);
            return Result<string>.Failure(ErrorCodes.WhatsAppSendFailed, "Failed to send WhatsApp message.");
        }
    }

    private static string NormaliseWhatsAppAddress(string phoneNumber)
    {
        var trimmed = phoneNumber.Trim();
        return trimmed.StartsWith(WhatsAppPrefix, StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{WhatsAppPrefix}{trimmed}";
    }
}

/// <summary>
/// Resolves WhatsApp Content template SIDs from Twilio settings.
/// </summary>
public sealed class TwilioWhatsAppTemplateCatalog : IWhatsAppTemplateCatalog
{
    private readonly TwilioSettings _settings;

    public TwilioWhatsAppTemplateCatalog(IOptions<TwilioSettings> options)
    {
        _settings = options.Value ?? new TwilioSettings();
    }

    public string? GetOtpTemplate()
    {
        var sid = _settings.WhatsApp.DefaultContentSid;
        return string.IsNullOrWhiteSpace(sid) ? null : sid;
    }
}
