using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Sms.Interfaces;

/// <summary>
/// Application-level SMS operations.
/// </summary>
public interface ISmsService
{
    /// <summary>
    /// Sends a transactional SMS.
    /// </summary>
    /// <param name="toPhoneNumber">Recipient phone number in E.164 format.</param>
    /// <param name="body">Plain-text message body.</param>
    Task<Result<string>> SendAsync(string toPhoneNumber, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a one-time password via SMS.
    /// </summary>
    Task<Result<string>> SendOtpAsync(string toPhoneNumber, string otpCode, CancellationToken cancellationToken = default);
}
