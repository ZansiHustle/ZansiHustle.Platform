using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Email.Mappers;

/// <summary>
/// Maps an email sender enum to concrete provider sender options.
/// </summary>
public interface IEmailSenderMapper
{
    /// <summary>
    /// Resolves sender options for the specified sender.
    /// </summary>
    Task<Result<EmailSenderOptions>> MapAsync(EmailSender sender);
}