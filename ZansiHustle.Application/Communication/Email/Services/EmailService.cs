using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Application.Communications.Email.Templates;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Email.Services;

/// <summary>
/// Handles application-level email workflows.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly IEmailProvider _emailProvider;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IEmailProvider emailProvider, ILogger<EmailService> logger)
    {
        _emailProvider = emailProvider;
        _logger = logger;
    }

    public async Task<Result> SendVerifyEmailAsync(string toEmail, string firstName, string verificationLink, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = AuthEmailTemplates.BuildVerifyEmail(firstName, verificationLink);

            var message = new EmailMessage
            {
                ToEmail = toEmail,
                ToName = firstName,
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Sender = EmailSender.Accounts
            };

            var result = await _emailProvider.SendAsync(message, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare verify-email message for {Email}.", toEmail);
            return Result.Failure(ErrorCodes.Exception, "Failed to send verification email.");
        }
    }

    public async Task<Result> SendEmailVerifiedConfirmationAsync(string toEmail, string firstName, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = AuthEmailTemplates.BuildEmailVerifiedConfirmation(firstName);

            var message = new EmailMessage
            {
                ToEmail = toEmail,
                ToName = firstName,
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Sender = EmailSender.Accounts
            };

            return await _emailProvider.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare email-verified confirmation for {Email}.", toEmail);
            return Result.Failure(ErrorCodes.Exception, "Failed to send email confirmation.");
        }
    }

    public async Task<Result> SendOtpAsync(string toEmail, string firstName, string otpCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = AuthEmailTemplates.BuildOtpEmail(firstName, otpCode);

            var message = new EmailMessage
            {
                ToEmail = toEmail,
                ToName = firstName,
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Sender = EmailSender.Security
            };

            return await _emailProvider.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare OTP email for {Email}.", toEmail);
            return Result.Failure(ErrorCodes.Exception, "Failed to send OTP email.");
        }
    }

    public async Task<Result> SendPasswordResetAsync(string toEmail, string firstName, string resetLink, CancellationToken cancellationToken = default)
    {
        try
        {
            var template = AuthEmailTemplates.BuildPasswordResetEmail(firstName, resetLink);

            var message = new EmailMessage
            {
                ToEmail = toEmail,
                ToName = firstName,
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Sender = EmailSender.Security
            };

            return await _emailProvider.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to prepare password-reset email for {Email}.", toEmail);
            return Result.Failure(ErrorCodes.Exception, "Failed to send password reset email.");
        }
    }
}