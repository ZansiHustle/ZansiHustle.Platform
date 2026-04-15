using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Communications.Sms.Interfaces;
using ZansiHustle.Application.Communications.Sms.Models;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Sms;

/// <summary>
/// Application-level SMS workflows.
/// </summary>
public sealed class SmsService : ISmsService
{
    private readonly ISmsProvider _provider;
    private readonly ILogger<SmsService> _logger;

    public SmsService(ISmsProvider provider, ILogger<SmsService> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task<Result<string>> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
            return Result<string>.Failure(ErrorCodes.BadRequest, "Recipient phone number is required.");
        if (string.IsNullOrWhiteSpace(body))
            return Result<string>.Failure(ErrorCodes.BadRequest, "SMS body is required.");

        var message = new SmsMessage
        {
            ToPhoneNumber = toPhoneNumber.Trim(),
            Body = body
        };

        try
        {
            return await _provider.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS send failed for {Phone}.", toPhoneNumber);
            return Result<string>.Failure(ErrorCodes.SmsSendFailed, "Failed to send SMS.");
        }
    }

    public Task<Result<string>> SendOtpAsync(string toPhoneNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        var body = $"Your ZansiHustle verification code is {otpCode}. It expires in a few minutes. Do not share this code with anyone.";
        return SendAsync(toPhoneNumber, body, cancellationToken);
    }
}
