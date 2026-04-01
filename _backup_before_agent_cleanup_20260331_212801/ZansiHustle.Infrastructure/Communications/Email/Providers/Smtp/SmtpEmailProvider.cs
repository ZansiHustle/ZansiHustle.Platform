using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Email.Mappers;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Infrastructure.Communications.Email.Providers.Smtp;

/// <summary>
/// SMTP implementation of the email provider.
/// </summary>
public sealed class SmtpEmailProvider : IEmailProvider
{
    private readonly IEmailSenderMapper _emailSenderMapper;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(
        IEmailSenderMapper emailSenderMapper,
        ILogger<SmtpEmailProvider> logger)
    {
        _emailSenderMapper = emailSenderMapper;
        _logger = logger;
    }

    public async Task<Result> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationResult = Validate(message);
            if (!validationResult.IsSuccess)
                return validationResult;

            var senderOptionsResult = await _emailSenderMapper.MapAsync(message.Sender);
            if (!senderOptionsResult.IsSuccess || senderOptionsResult.Data == null)
            {
                return Result.Failure(
                    senderOptionsResult.Code,
                    senderOptionsResult.Message);
            }

            var senderOptions = senderOptionsResult.Data;

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(senderOptions.FromEmail, senderOptions.FromName),
                Subject = message.Subject,
                Body = string.IsNullOrWhiteSpace(message.HtmlBody) ? message.PlainTextBody : message.HtmlBody,
                IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
            };

            mailMessage.To.Add(new MailAddress(message.ToEmail, message.ToName));

            if (!string.IsNullOrWhiteSpace(message.ReplyToEmail))
            {
                mailMessage.ReplyToList.Add(new MailAddress(message.ReplyToEmail));
            }

            if (message.CcEmails != null)
            {
                foreach (var ccEmail in message.CcEmails)
                    mailMessage.CC.Add(new MailAddress(ccEmail));
            }

            if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            {
                var plainView = AlternateView.CreateAlternateViewFromString(
                    message.PlainTextBody,
                    null,
                    "text/plain");

                mailMessage.AlternateViews.Add(plainView);
            }

            if (!string.IsNullOrWhiteSpace(message.HtmlBody))
            {
                var htmlView = AlternateView.CreateAlternateViewFromString(
                    message.HtmlBody,
                    null,
                    "text/html");

                mailMessage.AlternateViews.Add(htmlView);
            }

            using var smtpClient = new SmtpClient(senderOptions.Host, senderOptions.Port)
            {
                Credentials = new NetworkCredential(senderOptions.Username, senderOptions.Password),
                EnableSsl = senderOptions.EnableSsl
            };

            cancellationToken.ThrowIfCancellationRequested();

            await smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation(
                "SMTP email sent successfully to {Email} using sender {Sender}.",
                message.ToEmail,
                message.Sender);

            return Result.Success("Email sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SMTP email send failed for {Email} using sender {Sender}.",
                message.ToEmail,
                message.Sender);

            return Result.Failure(ErrorCodes.Exception, $"Failed to send email. {ex.Message}");
        }
    }

    private static Result Validate(EmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.ToEmail))
            return Result.Failure(ErrorCodes.BadRequest, "Recipient email is required.");

        if (string.IsNullOrWhiteSpace(message.Subject))
            return Result.Failure(ErrorCodes.BadRequest, "Email subject is required.");

        if (string.IsNullOrWhiteSpace(message.HtmlBody) && string.IsNullOrWhiteSpace(message.PlainTextBody))
            return Result.Failure(ErrorCodes.BadRequest, "Email body is required.");

        return Result.Success();
    }
}