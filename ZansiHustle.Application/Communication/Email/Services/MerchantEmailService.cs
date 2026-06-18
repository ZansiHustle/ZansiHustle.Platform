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
using ZansiHustle.Application.Communications.TestMode;
using ZansiHustle.Shared.Enums.Communications;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communication.Email.Services
{
    public class MerchantEmailService : IMerchantEmailService
    {
        // Official ZansiHustle seller-support address (configured SMTP sender:
        // EmailProviders:Senders:Support). Surfaced in the welcome email body as
        // the seller contact. If a separate contact address is ever needed,
        // promote this to config (e.g. Support:ContactEmail).
        private const string SellerSupportEmail = "support@zansihustle.co.za";

        private readonly IEmailProvider _emailProvider;
        private readonly ICommunicationRecipientResolver _recipients;
        private readonly ILogger<EmailService> _logger;

        public MerchantEmailService(
            IEmailProvider emailProvider,
            ICommunicationRecipientResolver recipients,
            ILogger<EmailService> logger)
        {
            _emailProvider = emailProvider;
            _recipients = recipients;
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
        public async Task<Result> SendSellerApprovalWelcomeEmailAsync(
            string toEmail, string? firstName, string? businessName, CancellationToken cancellationToken = default)
        {
            try
            {
                // Test-mode override (non-security purpose) — never touches OTP/security.
                var resolved = _recipients.ResolveEmail(toEmail, CommunicationPurpose.SellerApplicationApproved);
                if (string.IsNullOrWhiteSpace(resolved))
                    return Result.Failure("Recipient email is required.");

                var template = MerchantEmailTemplates.BuildSellerApprovalWelcomeEmail(
                    firstName ?? string.Empty, businessName, SellerSupportEmail);

                var message = new EmailMessage
                {
                    ToEmail = resolved!,
                    ToName = string.IsNullOrWhiteSpace(firstName) ? (businessName ?? "Seller") : firstName!,
                    Subject = template.Subject,
                    HtmlBody = template.HtmlBody,
                    PlainTextBody = template.PlainTextBody,
                    // From the seller-support mailbox so replies reach the right team.
                    Sender = EmailSender.Support
                };

                return await _emailProvider.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send seller-approval welcome email to {Email}.", toEmail);
                return Result.Failure(ErrorCodes.Exception, "Failed to send seller approval email.");
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
