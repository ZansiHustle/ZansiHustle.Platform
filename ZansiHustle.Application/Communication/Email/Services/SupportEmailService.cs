using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Application.Communications.Email.Templates;
using ZansiHustle.Application.Support;
using ZansiHustle.Application.Support.Dtos;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communication.Email.Services
{
    public class SupportEmailService : ISupportEmailService
    {
        private readonly IEmailProvider _emailProvider;
        private readonly ILogger<SupportEmailService> _logger;

        public SupportEmailService(IEmailProvider emailProvider, ILogger<SupportEmailService> logger)
        {
            _emailProvider = emailProvider;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Result> ProcessContactFormAsync(ContactRequestDto request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Message))
            {
                return Result.Failure("Name, email, and message are required.");
            }

            try
            {
                // 1. Send email to support team
                var supportEmailResult = await SendSupportEmailAsync(request);
                if (!supportEmailResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to send support email for {Email}: {Message}", request.Email, supportEmailResult.Message);
                }

                // 2. Send confirmation email to the user
                var userEmailResult = await SendUserConfirmationAsync(request);
                if (!userEmailResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to send user confirmation to {Email}: {Message}", request.Email, userEmailResult.Message);
                }

                // Return success even if one email fails (at least one should succeed)
                return Result.Success("Message sent successfully. We'll get back to you soon.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process contact form submission from {Email}", request.Email);
                return Result.Failure("Failed to send message. Please try again or email us directly at hello@zansihustle.co.za");
            }
        }

        private async Task<Result> SendSupportEmailAsync(ContactRequestDto request)
        {
            var template = SupportEmailTemplates.BuildSupportNotification(request);

            var message = new EmailMessage
            {
                ToEmail = "support@zansihustle.co.za",
                ToName = "ZansiHustle Support",
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Sender = EmailSender.NoReply,
                ReplyToEmail = request.Email
            };

            return await _emailProvider.SendAsync(message);
        }

        private async Task<Result> SendUserConfirmationAsync(ContactRequestDto request)
        {
            var template = SupportEmailTemplates.BuildUserConfirmation(request);

            var message = new EmailMessage
            {
                ToEmail = request.Email,
                ToName = request.Name,
                Subject = template.Subject,
                HtmlBody = template.HtmlBody,
                PlainTextBody = template.PlainTextBody,
                Sender = EmailSender.NoReply
            };

            return await _emailProvider.SendAsync(message);
        }
    }
}
