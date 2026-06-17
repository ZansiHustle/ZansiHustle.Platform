using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Communications.Email.Interfaces;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Application.Communications.Email.Templates;
using ZansiHustle.Application.Communications.Sms.Interfaces;
using ZansiHustle.Application.Communications.TestMode;
using ZansiHustle.Shared.Enums.Communications;

namespace ZansiHustle.Application.Communications.Email.Services;

/// <summary>
/// Sends the post-booking "shipment booked" NOTIFICATIONS — email (always) plus
/// a best-effort SMS — for both the customer and the seller. All recipients flow
/// through <see cref="ICommunicationRecipientResolver"/> (test-mode override +
/// masked logging); no test-mode config is read here. The seller messages carry
/// the buyer NAME + delivery AREA only — never the buyer's phone/email/full address.
/// (Interface name retained as IShipmentEmailService to avoid call-site churn.)
/// </summary>
public sealed class ShipmentEmailService : IShipmentEmailService
{
    private readonly IEmailProvider _emailProvider;
    private readonly ISmsService _smsService;
    private readonly ICommunicationRecipientResolver _recipients;
    private readonly ILogger<ShipmentEmailService> _logger;

    public ShipmentEmailService(
        IEmailProvider emailProvider,
        ISmsService smsService,
        ICommunicationRecipientResolver recipients,
        ILogger<ShipmentEmailService> logger)
    {
        _emailProvider = emailProvider;
        _smsService = smsService;
        _recipients = recipients;
        _logger = logger;
    }

    public async Task SendShipmentBookedEmailsAsync(ShipmentEmailContext context, CancellationToken cancellationToken = default)
    {
        if (context is null) return;

        // ── Email (recipients via central resolver) ──────────────────────────
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

        // ── SMS (best-effort; recipients via the SAME resolver) ──────────────
        var customerSms = _recipients.ResolveSms(context.CustomerPhone, CommunicationPurpose.ShipmentCustomer);
        if (!string.IsNullOrWhiteSpace(customerSms))
            await SafeSendSmsAsync(customerSms!, BuildCustomerSms(context), "customer", context.OrderCode, cancellationToken);

        var sellerSms = _recipients.ResolveSms(context.SellerPhone, CommunicationPurpose.ShipmentSeller);
        if (!string.IsNullOrWhiteSpace(sellerSms))
            await SafeSendSmsAsync(sellerSms!, BuildSellerSms(context), "seller", context.OrderCode, cancellationToken);
    }

    // ── SMS bodies — concise, and the SELLER one carries NO buyer phone/email/
    //    full street address (buyer NAME + delivery AREA only). ───────────────
    private static string BuildCustomerSms(ShipmentEmailContext c)
    {
        var parts = new System.Collections.Generic.List<string> { $"ZansiHustle order {c.OrderCode}: your parcel is being collected." };
        if (!string.IsNullOrWhiteSpace(c.TrackingReference)) parts.Add($"Tracking {c.TrackingReference}.");
        var range = FormatRange(c.ExpectedDeliveryFrom, c.ExpectedDeliveryTo);
        if (!string.IsNullOrWhiteSpace(range)) parts.Add($"Expected delivery {range}.");
        parts.Add("Track in the app.");
        return string.Join(" ", parts);
    }

    private static string BuildSellerSms(ShipmentEmailContext c)
    {
        var parts = new System.Collections.Generic.List<string> { $"ZansiHustle order {c.OrderCode}: courier pickup booked." };
        if (!string.IsNullOrWhiteSpace(c.TrackingReference)) parts.Add($"Tracking {c.TrackingReference}.");
        var coll = FormatDay(c.ExpectedCollectionDate);
        if (!string.IsNullOrWhiteSpace(coll)) parts.Add($"Expected collection {coll}.");
        if (!string.IsNullOrWhiteSpace(c.CustomerName)) parts.Add($"Buyer {c.CustomerName}.");
        if (!string.IsNullOrWhiteSpace(c.DeliveryArea)) parts.Add($"Delivery area {c.DeliveryArea}.");
        parts.Add("Please have the parcel ready.");
        return string.Join(" ", parts);
    }

    private static string FormatDay(DateTime? d)
        => d is null ? string.Empty : d.Value.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    private static string FormatRange(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return string.Empty;
        if (from is not null && to is not null && from.Value.Date != to.Value.Date)
            return $"{FormatDay(from)} – {FormatDay(to)}";
        return FormatDay(from ?? to);
    }

    private async Task SafeSendSmsAsync(string toPhone, string body, string role, string orderCode, CancellationToken ct)
    {
        try
        {
            var result = await _smsService.SendAsync(toPhone, body, ct);
            if (!result.IsSuccess)
                _logger.LogWarning("Shipment {Role} SMS not sent (order={Order}): {Msg}", role, orderCode, result.Message);
            else
                _logger.LogInformation("Shipment {Role} SMS sent (order={Order}).", role, orderCode);
        }
        catch (Exception ex)
        {
            // Best-effort — never break the booking flow on an SMS failure.
            _logger.LogError(ex, "Shipment {Role} SMS threw (order={Order}).", role, orderCode);
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
