using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using ZansiHustle.Application.Communications.Sms.Interfaces;
using ZansiHustle.Application.Communications.Sms.Models;
using ZansiHustle.Infrastructure.Communications.Twilio;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Communications.Sms.Providers.Twilio;

/// <summary>
/// Twilio implementation of <see cref="ISmsProvider"/>.
/// </summary>
public sealed class TwilioSmsProvider : ISmsProvider
{
    private readonly ITwilioClientProvider _clientProvider;
    private readonly TwilioSettings _settings;
    private readonly ILogger<TwilioSmsProvider> _logger;

    public TwilioSmsProvider(ITwilioClientProvider clientProvider, IOptions<TwilioSettings> options, ILogger<TwilioSmsProvider> logger)
    {
        _clientProvider = clientProvider;
        _settings = options.Value ?? new TwilioSettings();
        _logger = logger;
    }

    public async Task<Result<string>> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message.ToPhoneNumber))
            return Result<string>.Failure(ErrorCodes.BadRequest, "Recipient phone number is required.");

        if (string.IsNullOrWhiteSpace(message.Body))
            return Result<string>.Failure(ErrorCodes.BadRequest, "SMS body is required.");

        if (!_clientProvider.IsConfigured)
            return Result<string>.Failure(ErrorCodes.ProviderNotConfigured, "Twilio is not configured. Set Twilio:AccountSid and Twilio:AuthToken.");

        if (!_settings.Sms.HasSender())
            return Result<string>.Failure(ErrorCodes.ProviderNotConfigured, "Twilio SMS sender is not configured.");

        var client = _clientProvider.GetClient();

        if (client is null)
            return Result<string>.Failure(ErrorCodes.ProviderNotConfigured, "Twilio client could not be initialised.");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new CreateMessageOptions(new PhoneNumber(message.ToPhoneNumber))
            {
                Body = message.Body
            };

            if (!string.IsNullOrWhiteSpace(_settings.Sms.MessagingServiceSid))
            {
                options.MessagingServiceSid = _settings.Sms.MessagingServiceSid;
            }
            else
            {
                options.From = new PhoneNumber(_settings.Sms.FromPhoneNumber);
            }

            var resource = await MessageResource.CreateAsync(options, client);

            _logger.LogInformation("Twilio SMS queued. Sid={Sid}, To={To}, Status={Status}.", resource.Sid, message.ToPhoneNumber, resource.Status);

            if (resource.ErrorCode is not null)
            {
                _logger.LogWarning("Twilio returned an error for SMS {Sid}: {Code} {Message}.", resource.Sid, resource.ErrorCode, resource.ErrorMessage);
                return Result<string>.Failure(ErrorCodes.SmsSendFailed, resource.ErrorMessage ?? "Twilio rejected the SMS.");
            }

            return Result<string>.Success(resource.Sid, "SMS queued for delivery.");
        }
        catch (global::Twilio.Exceptions.ApiException ex)
        {
            _logger.LogError(ex, "Twilio API exception while sending SMS to {Phone}. Code={Code}.", message.ToPhoneNumber, ex.Code);
            return Result<string>.Failure(ErrorCodes.SmsSendFailed, $"Twilio error {ex.Code}: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected SMS send failure for {Phone}.", message.ToPhoneNumber);
            return Result<string>.Failure(ErrorCodes.SmsSendFailed, "Failed to send SMS.");
        }
    }
}
