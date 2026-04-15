using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Communications.WhatsApp.Interfaces;
using ZansiHustle.Application.Communications.WhatsApp.Models;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.WhatsApp;

/// <summary>
/// Application-level WhatsApp workflows.
/// </summary>
public sealed class WhatsAppService : IWhatsAppService
{
    private readonly IWhatsAppProvider _provider;
    private readonly IWhatsAppTemplateCatalog _templates;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IWhatsAppProvider provider,
        IWhatsAppTemplateCatalog templates,
        ILogger<WhatsAppService> logger)
    {
        _provider = provider;
        _templates = templates;
        _logger = logger;
    }

    public async Task<Result<string>> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
            return Result<string>.Failure(ErrorCodes.BadRequest, "Recipient phone number is required.");
        if (string.IsNullOrWhiteSpace(body))
            return Result<string>.Failure(ErrorCodes.BadRequest, "WhatsApp body is required.");

        try
        {
            return await _provider.SendAsync(new WhatsAppMessage
            {
                ToPhoneNumber = toPhoneNumber.Trim(),
                Body = body
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp send failed for {Phone}.", toPhoneNumber);
            return Result<string>.Failure(ErrorCodes.WhatsAppSendFailed, "Failed to send WhatsApp message.");
        }
    }

    public async Task<Result<string>> SendTemplateAsync(
        string toPhoneNumber,
        string contentSid,
        string? contentVariables,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toPhoneNumber))
            return Result<string>.Failure(ErrorCodes.BadRequest, "Recipient phone number is required.");
        if (string.IsNullOrWhiteSpace(contentSid))
            return Result<string>.Failure(ErrorCodes.BadRequest, "Content template SID is required.");

        try
        {
            return await _provider.SendAsync(new WhatsAppMessage
            {
                ToPhoneNumber = toPhoneNumber.Trim(),
                ContentSid = contentSid,
                ContentVariables = contentVariables
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp template send failed for {Phone} sid={Sid}.", toPhoneNumber, contentSid);
            return Result<string>.Failure(ErrorCodes.WhatsAppSendFailed, "Failed to send WhatsApp message.");
        }
    }

    public Task<Result<string>> SendOtpAsync(string toPhoneNumber, string otpCode, CancellationToken cancellationToken = default)
    {
        var otpTemplate = _templates.GetOtpTemplate();

        if (otpTemplate is not null)
        {
            // Default template contract: variable {{1}} is the OTP code.
            var variables = "{\"1\":\"" + otpCode + "\"}";
            return SendTemplateAsync(toPhoneNumber, otpTemplate, variables, cancellationToken);
        }

        // Fallback: free-form text. Only works inside a 24h customer-service window.
        var body = $"Your ZansiHustle verification code is {otpCode}. It expires in a few minutes. Do not share this code.";
        return SendAsync(toPhoneNumber, body, cancellationToken);
    }
}

/// <summary>
/// Resolves configured WhatsApp Content API template SIDs by purpose.
/// Implementations live in Infrastructure.
/// </summary>
public interface IWhatsAppTemplateCatalog
{
    /// <summary>Returns the configured OTP template SID, or null if not configured.</summary>
    string? GetOtpTemplate();
}
