using ZansiHustle.Application.Communications.Email.Mappers;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;
using ZansiHustle.Infrastructure.Communications.Email.Directories;

namespace ZansiHustle.Infrastructure.Communications.Email.Mappers;

/// <summary>
/// Maps sender enums to concrete email sender options.
/// </summary>
public sealed class EmailSenderMapper : IEmailSenderMapper
{
    public Task<Result<EmailSenderOptions>> MapAsync(EmailSender sender)
    {
        try
        {
            var options = sender switch
            {
                EmailSender.Accounts => Build(
                    EmailSenderDirectory.Accounts.Host,
                    EmailSenderDirectory.Accounts.Port,
                    EmailSenderDirectory.Accounts.Username,
                    EmailSenderDirectory.Accounts.Password,
                    EmailSenderDirectory.Accounts.FromEmail,
                    EmailSenderDirectory.Accounts.FromName,
                    EmailSenderDirectory.Accounts.EnableSsl),

                EmailSender.Support => Build(
                    EmailSenderDirectory.Support.Host,
                    EmailSenderDirectory.Support.Port,
                    EmailSenderDirectory.Support.Username,
                    EmailSenderDirectory.Support.Password,
                    EmailSenderDirectory.Support.FromEmail,
                    EmailSenderDirectory.Support.FromName,
                    EmailSenderDirectory.Support.EnableSsl),

                EmailSender.NoReply => Build(
                    EmailSenderDirectory.NoReply.Host,
                    EmailSenderDirectory.NoReply.Port,
                    EmailSenderDirectory.NoReply.Username,
                    EmailSenderDirectory.NoReply.Password,
                    EmailSenderDirectory.NoReply.FromEmail,
                    EmailSenderDirectory.NoReply.FromName,
                    EmailSenderDirectory.NoReply.EnableSsl),

                EmailSender.Security => Build(
                    EmailSenderDirectory.Security.Host,
                    EmailSenderDirectory.Security.Port,
                    EmailSenderDirectory.Security.Username,
                    EmailSenderDirectory.Security.Password,
                    EmailSenderDirectory.Security.FromEmail,
                    EmailSenderDirectory.Security.FromName,
                    EmailSenderDirectory.Security.EnableSsl),

                EmailSender.Payments => Build(
                    EmailSenderDirectory.Payments.Host,
                    EmailSenderDirectory.Payments.Port,
                    EmailSenderDirectory.Payments.Username,
                    EmailSenderDirectory.Payments.Password,
                    EmailSenderDirectory.Payments.FromEmail,
                    EmailSenderDirectory.Payments.FromName,
                    EmailSenderDirectory.Payments.EnableSsl),

                _ => Build(
                    EmailSenderDirectory.NoReply.Host,
                    EmailSenderDirectory.NoReply.Port,
                    EmailSenderDirectory.NoReply.Username,
                    EmailSenderDirectory.NoReply.Password,
                    EmailSenderDirectory.NoReply.FromEmail,
                    EmailSenderDirectory.NoReply.FromName,
                    EmailSenderDirectory.NoReply.EnableSsl)
            };

            return Task.FromResult(Result<EmailSenderOptions>.Success(options));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<EmailSenderOptions>.Failure(
                ErrorCodes.Exception,
                $"Failed to resolve email sender options. {ex.Message}"));
        }
    }

    private static EmailSenderOptions Build(string host, int port, string username, string password,string fromEmail, string fromName, bool enableSsl)
    {
        return new EmailSenderOptions
        {
            Host = host,
            Port = port,
            Username = username,
            Password = password,
            FromEmail = fromEmail,
            FromName = fromName,
            EnableSsl = enableSsl
        };
    }
}