using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZansiHustle.Application.Communication.Email.Interfaces;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Application.Communications.Email.Services;
using ZansiHustle.Application.Communications.Email.Templates;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communication.Email.Services
{
    public class MerchantEmailService : IMerchantEmailService
    {
        private readonly IEmailProvider _emailProvider;
        private readonly ILogger<EmailService> _logger;

        public MerchantEmailService(IEmailProvider emailProvider, ILogger<EmailService> logger)
        {
            _emailProvider = emailProvider;
            _logger = logger;
        }

        // Add to EmailService.cs
        /// <inheritdoc />
        public async Task<Result> SendLeadWelcomeEmailAsync(string toEmail, string firstName, string? businessName = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    return Result.Failure("Recipient email is required.");
                }

                var template = MerchantEmailTemplates.BuildVipWelcomeEmail(firstName, businessName);

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
                _logger.LogError(ex, "Failed to send lead welcome email to {Email}.", toEmail);
                return Result.Failure(ErrorCodes.Exception, "Failed to send welcome email.");
            }
        }

        /// <inheritdoc />
        public async Task<Result> SendNewLeadNotificationAsync(string leadName, string phone, string? email, string? category, string? province, string? referrerName, CancellationToken cancellationToken = default)
        {
            try
            {
                var template = MerchantEmailTemplates.BuildNewLeadNotification(leadName, phone, email, category, province, referrerName);

                var message = new EmailMessage
                {
                    ToEmail = "leads@zansihustle.co.za",
                    ToName = "ZansiHustle Team",
                    Subject = template.Subject,
                    HtmlBody = template.HtmlBody,
                    PlainTextBody = template.PlainTextBody,
                    Sender = EmailSender.NoReply
                };

                return await _emailProvider.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send new lead notification for {LeadName}.", leadName);
                return Result.Failure(ErrorCodes.Exception, "Failed to send notification.");
            }
        }
    }
}
