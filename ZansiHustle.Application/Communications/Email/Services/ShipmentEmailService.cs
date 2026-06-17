using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Application.Communications.Email.Templates;
using ZansiHustle.Application.Communications.TestMode;
using ZansiHustle.Shared.Enums.Communications;

namespace ZansiHustle.Application.Communications.Email.Services;

/// <inheritdoc />
public sealed class ShipmentEmailService : IShipmentEmailService
{
    private readonly IEmailProvider _emailProvider;
    private readonly ICommunicationRecipientResolver _recipients;
    private readonly ILogger<ShipmentEmailService> _logger;

    public ShipmentEmailService(
        IEmailProvider emailProvider,
        ICommunicationRecipientResolver recipients,
        ILogger<ShipmentEmailService> logger)
    {
        _emailProvider = emailProvider;
        _recipients = recipients;
        _logger = logger;
    }

    public async Task SendShipmentBookedEmailsAsync(ShipmentEmailContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) return;

        // Recipients flow through the central resolver — NO direct test-mode
        // config reads here. The resolver applies any override + logs it (masked).
        var customerTo = _recipients.ResolveEmail(context.CustomerEmail, CommunicationPurpose.ShipmentCustomer);
        if (!string.IsNullOrWhiteSpace(customerTo))
        {
            var t = ShipmentEmailTemplates.BuildCustomerShipmentBooked(context);
            await SafeSendAsync(customerTo!, context.CustomerName, t, EmailSender.NoReply, "customer", context.OrderCode, cancellationToken);
        }

        var sellerTo = _recipients.ResolveEmail(context.SellerEmail, CommunicationPurpose.ShipmentSeller);
        if (!string.IsNullOrWhiteSpace(sellerTo))
        {
            var t = ShipmentEmailTemplates.BuildSellerShipmentBooked(context);
            await SafeSendAsync(sellerTo!, context.SellerName, t, EmailSender.NoReply, "seller", context.OrderCode, cancellationToken);
        }
    }

    private async Task SafeSendAsync(
        string toEmail, string? toName, (string Subject, string HtmlBody, string PlainTextBody) tpl,
        EmailSender sender, string role, string orderCode, CancellationToken ct)
    {
        try
        {
            var msg = new EmailMessage
            {
                ToEmail = toEmail,
                ToName = toName,
                Subject = tpl.Subject,
                HtmlBody = tpl.HtmlBody,
                PlainTextBody = tpl.PlainTextBody,
                Sender = sender,
            };
            var result = await _emailProvider.SendAsync(msg, ct);
            if (!result.IsSuccess)
                _logger.LogWarning("Shipment {Role} email not sent (order={Order}): {Msg}", role, orderCode, result.Message);
            else
                _logger.LogInformation("Shipment {Role} email sent (order={Order}).", role, orderCode);
        }
        catch (Exception ex)
        {
            // Best-effort — never break the booking flow on an email failure.
            _logger.LogError(ex, "Shipment {Role} email threw (order={Order}).", role, orderCode);
        }
    }
}
