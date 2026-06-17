using System;
using System.Globalization;
using System.Net;
using System.Text;
using ZansiHustle.Application.Communications.Email.Models;

namespace ZansiHustle.Application.Communications.Email.Templates;

/// <summary>
/// "Shipment booked with courier" emails — one tailored for the CUSTOMER
/// (on its way + ETA + their delivery address) and one for the SELLER (pickup
/// scheduled, prepare the parcel — buyer NAME + delivery AREA only, never the
/// buyer's email/phone/full street address).
/// </summary>
public static class ShipmentEmailTemplates
{
    private const string Primary = "#16A34A";
    private const string Text = "#111827";
    private const string TextMuted = "#6B7280";
    private const string Border = "#E5E7EB";
    private const string Surface = "#F8FAFC";
    private const string White = "#FFFFFF";

    // ── Customer ────────────────────────────────────────────────────────────
    public static (string Subject, string HtmlBody, string PlainTextBody) BuildCustomerShipmentBooked(ShipmentEmailContext c)
    {
        var subject = $"Your ZansiHustle order {c.OrderCode} is on the way";
        var rows = new StringBuilder();
        Row(rows, "Order", c.OrderCode);
        Row(rows, "Seller", c.SellerName);
        Row(rows, "Tracking ref", c.TrackingReference);
        Row(rows, "Status", c.Status);
        Row(rows, "Expected collection", FormatDate(c.ExpectedCollectionDate));
        Row(rows, "Expected delivery", FormatRange(c.ExpectedDeliveryFrom, c.ExpectedDeliveryTo));
        Row(rows, "Delivery address", c.DeliveryAddressFull);

        var html = Wrap(
            title: "Your order is on the way",
            intro: "Good news — the courier has been booked and is preparing to collect your order from the seller. Here are the details:",
            rowsHtml: rows.ToString(),
            footer: "You can track your order any time in the ZansiHustle app under My Orders. Need help? Reply to this email and our support team will assist.");

        var text = new StringBuilder();
        text.AppendLine($"Your ZansiHustle order {c.OrderCode} is on the way.");
        text.AppendLine();
        PlainRow(text, "Seller", c.SellerName);
        PlainRow(text, "Tracking ref", c.TrackingReference);
        PlainRow(text, "Status", c.Status);
        PlainRow(text, "Expected collection", FormatDate(c.ExpectedCollectionDate));
        PlainRow(text, "Expected delivery", FormatRange(c.ExpectedDeliveryFrom, c.ExpectedDeliveryTo));
        PlainRow(text, "Delivery address", c.DeliveryAddressFull);
        text.AppendLine();
        text.AppendLine("Track your order in the ZansiHustle app under My Orders.");
        return (subject, html, text.ToString());
    }

    // ── Seller ──────────────────────────────────────────────────────────────
    public static (string Subject, string HtmlBody, string PlainTextBody) BuildSellerShipmentBooked(ShipmentEmailContext c)
    {
        var subject = $"Courier pickup scheduled for your ZansiHustle order {c.OrderCode}";
        var rows = new StringBuilder();
        Row(rows, "Order", c.OrderCode);
        Row(rows, "Tracking ref", c.TrackingReference);
        Row(rows, "Courier", c.Courier);
        Row(rows, "Service", c.ServiceLevel);
        Row(rows, "Expected collection", FormatDate(c.ExpectedCollectionDate));
        Row(rows, "Pickup address", c.PickupAddress);
        Row(rows, "Buyer", c.CustomerName);          // NAME ONLY
        Row(rows, "Delivery area", c.DeliveryArea);   // BROAD AREA ONLY

        var html = Wrap(
            title: "Courier pickup scheduled",
            intro: "A courier collection has been booked for one of your ZansiHustle orders. Please have the parcel packed and ready for collection on the expected collection date.",
            rowsHtml: rows.ToString(),
            footer: "ZansiHustle manages delivery end-to-end. You don't need to arrange anything with the courier — just have the parcel ready. Questions? Reply to this email.");

        var text = new StringBuilder();
        text.AppendLine($"Courier pickup scheduled for your ZansiHustle order {c.OrderCode}.");
        text.AppendLine();
        PlainRow(text, "Tracking ref", c.TrackingReference);
        PlainRow(text, "Courier", c.Courier);
        PlainRow(text, "Service", c.ServiceLevel);
        PlainRow(text, "Expected collection", FormatDate(c.ExpectedCollectionDate));
        PlainRow(text, "Pickup address", c.PickupAddress);
        PlainRow(text, "Buyer", c.CustomerName);
        PlainRow(text, "Delivery area", c.DeliveryArea);
        text.AppendLine();
        text.AppendLine("Please have the parcel packed and ready for collection.");
        return (subject, html, text.ToString());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void Row(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.Append($@"<tr>
            <td style='padding:8px 0; color:{TextMuted}; font-size:13px; width:160px; vertical-align:top;'>{WebUtility.HtmlEncode(label)}</td>
            <td style='padding:8px 0; color:{Text}; font-size:14px; font-weight:600;'>{WebUtility.HtmlEncode(value)}</td>
        </tr>");
    }

    private static void PlainRow(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.AppendLine($"{label}: {value}");
    }

    private static string FormatDate(DateTime? d)
        => d is null ? string.Empty : d.Value.ToString("ddd, dd MMM yyyy", CultureInfo.InvariantCulture);

    private static string FormatRange(DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return string.Empty;
        if (from is not null && to is not null && from.Value.Date != to.Value.Date)
            return $"{FormatDate(from)} – {FormatDate(to)}";
        return FormatDate(from ?? to);
    }

    private static string Wrap(string title, string intro, string rowsHtml, string footer)
        => $@"
<div style='background:{Surface}; padding:24px; font-family:Segoe UI,Roboto,Helvetica,Arial,sans-serif;'>
  <div style='max-width:560px; margin:0 auto; background:{White}; border:1px solid {Border}; border-radius:14px; overflow:hidden;'>
    <div style='background:{Primary}; padding:18px 24px;'>
      <span style='color:{White}; font-size:18px; font-weight:800;'>ZansiHustle</span>
    </div>
    <div style='padding:24px;'>
      <h1 style='margin:0 0 8px; font-size:20px; color:{Text};'>{WebUtility.HtmlEncode(title)}</h1>
      <p style='margin:0 0 18px; color:{TextMuted}; font-size:14px; line-height:21px;'>{WebUtility.HtmlEncode(intro)}</p>
      <table style='width:100%; border-collapse:collapse; border-top:1px solid {Border};'>{rowsHtml}</table>
      <p style='margin:20px 0 0; color:{TextMuted}; font-size:13px; line-height:19px;'>{WebUtility.HtmlEncode(footer)}</p>
    </div>
  </div>
</div>";
}
