// ZansiHustle.Application/Communications/Email/Templates/MerchantEmailTemplates.cs
using System.Net;

namespace ZansiHustle.Application.Communications.Email.Templates;

/// <summary>
/// Builds ZansiHustle merchant/lead email templates.
/// </summary>
public static class MerchantEmailTemplates
{
    private const string Background = "#FFFFFF";
    private const string Surface = "#F8FAFC";
    private const string Surface2 = "#F1F5F9";
    private const string Primary = "#16A34A";
    private const string Text = "#111827";
    private const string TextMuted = "#6B7280";
    private const string Border = "#E5E7EB";
    private const string Success = "#16A34A";
    private const string White = "#FFFFFF";
    private const string Black = "#0F172A";
    private const string LightGray = "#58595A";
    private const string Gold = "#F59E0B";

    /// <summary>
    /// Builds the VIP welcome email for early access leads.
    /// </summary>
    public static (string Subject, string HtmlBody, string PlainTextBody) BuildVipWelcomeEmail(string firstName, string? businessName = null)
    {
        var safeName = Safe(firstName);
        var displayName = string.IsNullOrWhiteSpace(businessName) ? safeName : businessName;
        var subject = $"🎉 You're in! Welcome to the ZansiHustle VIP Early Access List";

        var htmlBody = Wrap(
            firstName: safeName,
            title: "You're on the VIP List!",
            subtitle: "Welcome to the ZansiHustle Early Access Community",
            body: $@"
                <p>Congratulations {WebUtility.HtmlEncode(safeName)}! 🎉</p>
                
                <p>You've been selected for <strong style='color:{Gold};'>VIP early access</strong> to ZansiHustle — South Africa's newest platform for hustlers, sellers, and service providers.</p>
                
                <div style='background: linear-gradient(135deg, {Success}10 0%, {Success}05 100%); border: 1px solid {Success}30; border-radius: 16px; padding: 24px; margin: 24px 0; text-align: center;'>
                    <div style='font-size: 48px; margin-bottom: 12px;'>🚀</div>
                    <h3 style='margin: 0 0 8px 0; color: {Success}; font-size: 18px;'>You're ahead of the crowd</h3>
                    <p style='margin: 0; color: {TextMuted}; font-size: 14px;'>As a VIP early access member, you'll be among the first South Africans to launch your business on ZansiHustle when we go live.</p>
                </div>
                
                <h3 style='margin: 24px 0 12px 0; color: {Text}; font-size: 16px;'>What happens next?</h3>
                <ul style='margin: 0 0 24px 0; padding-left: 20px; color: {TextMuted}; line-height: 1.6;'>
                    <li style='margin-bottom: 8px;'>📧 <strong>Stay tuned</strong> — We'll email you exclusive updates about our launch progress</li>
                    <li style='margin-bottom: 8px;'>🎁 <strong>Priority onboarding</strong> — You'll get early access to set up your profile before the public launch</li>
                    <li style='margin-bottom: 8px;'>⭐ <strong>Featured status</strong> — Early members get premium placement in the app directory</li>
                    <li style='margin-bottom: 8px;'>💬 <strong>Community access</strong> — Join our private WhatsApp group for early hustlers</li>
                </ul>
                
                <div style='background-color: {Surface}; border-left: 4px solid {Success}; padding: 16px; margin: 24px 0; border-radius: 8px;'>
                    <p style='margin: 0 0 8px 0; font-weight: bold; color: {Text};'>✨ Your VIP Benefits:</p>
                    <p style='margin: 0; color: {TextMuted}; font-size: 14px;'>
                        • Free lifetime profile on the platform<br />
                        • Priority support from our team<br />
                        • Early access to new features<br />
                        • Marketing spotlight opportunities
                    </p>
                </div>
                
                <p>We're building something special for South African hustlers, and we're excited to have you with us from day one.</p>
                
                <p style='margin-top: 24px;'>Got questions? Reply to this email — we read every message personally.</p>",
            footerTitle: "ZansiHustle VIP Team",
            showVipBadge: true);

        var plainTextBody = $@"
            Congratulations {safeName}! 🎉

            You've been selected for VIP early access to ZansiHustle — South Africa's newest platform for hustlers, sellers, and service providers.

            ✨ You're ahead of the crowd
            As a VIP early access member, you'll be among the first South Africans to launch your business on ZansiHustle when we go live.

            What happens next?

            📧 Stay tuned — We'll email you exclusive updates about our launch progress
            🎁 Priority onboarding — You'll get early access to set up your profile before the public launch
            ⭐ Featured status — Early members get premium placement in the app directory
            💬 Community access — Join our private WhatsApp group for early hustlers

            ✨ Your VIP Benefits:
            • Free lifetime profile on the platform
            • Priority support from our team
            • Early access to new features
            • Marketing spotlight opportunities

            We're building something special for South African hustlers, and we're excited to have you with us from day one.

            Got questions? Reply to this email — we read every message personally.

            ZansiHustle VIP Team";

        return (subject, htmlBody, plainTextBody);
    }

    /// <summary>
    /// Builds the seller-approval welcome + compliance onboarding email, sent
    /// once when an admin approves a seller/merchant application. Warm + clear
    /// expectations (selling standards, order acceptance, delivery/pickup,
    /// service-provider conduct, safety, shops). Never contains admin notes.
    /// </summary>
    public static (string Subject, string HtmlBody, string PlainTextBody) BuildSellerApprovalWelcomeEmail(
        string firstName, string? businessName, string supportEmail)
    {
        var safeName = Safe(firstName);
        var who = string.IsNullOrWhiteSpace(businessName) ? safeName : businessName!;
        var subject = "Welcome to ZansiHustle — Your seller account is approved";
        var support = string.IsNullOrWhiteSpace(supportEmail) ? "support@zansihustle.co.za" : supportEmail.Trim();
        var supportEnc = WebUtility.HtmlEncode(support);

        string H(string t) => $"<h3 style='margin:26px 0 10px 0; color:{Text}; font-size:16px;'>{WebUtility.HtmlEncode(t)}</h3>";
        string UL(params string[] items)
        {
            var sb = new System.Text.StringBuilder(
                $"<ul style='margin:0 0 4px 0; padding-left:20px; color:{TextMuted}; line-height:1.7; font-size:14px;'>");
            foreach (var i in items) sb.Append($"<li style='margin-bottom:6px;'>{i}</li>");
            sb.Append("</ul>");
            return sb.ToString();
        }

        var body = $@"
            <p>Congratulations {WebUtility.HtmlEncode(who)} — your ZansiHustle seller account has been <strong style='color:{Primary};'>approved</strong>. You can now start listing products or offering services and receiving customer requests on the platform.</p>

            <div style='background-color:{Surface}; border-left:4px solid {Primary}; padding:16px; margin:20px 0; border-radius:8px;'>
                <p style='margin:0; color:{TextMuted}; font-size:14px;'>To keep ZansiHustle safe and trusted for buyers and sellers, here's what we expect from approved sellers. A few minutes now saves headaches later.</p>
            </div>

            {H("What approval means")}
            {UL(
                "You're cleared to sell and list on ZansiHustle.",
                "Keep your listings, prices and availability accurate and up to date.",
                "Approval can be reviewed if there are repeated complaints, unsafe conduct, fraud, or policy breaches.")}

            {H("Product selling standards")}
            {UL(
                "Only list products you actually have or can fulfil.",
                "Use accurate photos, prices, descriptions, sizes, condition and stock status.",
                "Never list broken, unsafe, counterfeit, stolen, illegal, misleading or restricted items.",
                "For second-hand items, describe the condition clearly and honestly.",
                "Broken, damaged or misrepresented products may be returned or refunded.",
                "Repeated customer complaints can affect your listing visibility or account status.")}

            {H("Accepting orders")}
            {UL(
                "Only accept an order when you're sure you can fulfil it.",
                "Don't accept if the item is unavailable, damaged, or you can't prepare it for collection.",
                "Repeated rejections, missed pickups or unfulfilled orders may affect your seller trust and listing performance.",
                "Prepare parcels properly and on time — if a courier pickup is scheduled, have the parcel ready.")}

            {H("Delivery &amp; pickup")}
            {UL(
                "For product orders, ZansiHustle may arrange delivery through its dispatch partners.",
                "Confirm your correct pickup address and package items safely.",
                "Don't miss a scheduled courier pickup.",
                "Use the in-app flow for delivery — please don't arrange private off-platform delivery or contact the customer directly unless the platform flow allows it.",
                "Respect customers' private contact and address information.")}

            {H("Service provider standards")}
            {UL(
                "Only accept service bookings you can honour, and arrive on time — or communicate early if there's an issue.",
                "Keep service descriptions, pricing, house-call/travel fees, availability and visit-shop options accurate.",
                "For house calls, respect customer safety and privacy; for visit-shop services keep your location and operating details accurate.",
                "Act professionally; never request unsafe, inappropriate or off-platform arrangements.",
                "Mark a service complete only when the work was genuinely done.")}

            {H("Safety &amp; conduct")}
            {UL(
                "Your safety and your customers' safety both matter — keep communication respectful and meet/serve in safe environments.",
                "Report suspicious behaviour, and don't share customer information beyond what's needed to fulfil an order.",
                "ZansiHustle may step in where there are disputes, safety reports, fraud concerns or repeated complaints.")}

            {H("Your shop on ZansiHustle")}
            {UL(
                "Your shop is your public seller storefront — it shows customers your brand, products/services, area and credibility.",
                "A complete shop profile groups your listings under one professional identity and builds customer confidence.",
                "Manage your products and services under your shop where supported.")}

            {H("How to succeed")}
            {UL(
                "Keep listings updated and respond quickly to requests.",
                "Accept only what you can fulfil, and package products properly.",
                "Keep service appointments professional, use clear photos, and keep prices and fees honest.",
                "Watch your notifications, and contact support whenever you're unsure.")}

            {H("Support")}
            <p style='margin:0 0 4px 0; color:{TextMuted}; font-size:14px;'>Questions or something not working? We're here to help:</p>
            {UL($"Email: <a href='mailto:{supportEnc}' style='color:{Primary}; text-decoration:none;'>{supportEnc}</a>")}

            <p style='margin-top:24px; font-size:12px; color:{TextMuted}; line-height:1.6;'>
                ZansiHustle may update its seller guidelines over time. Continued use of the platform means following the current platform rules and marketplace standards. This email is onboarding guidance, not a full legal contract — please refer to the in-app and website policies for the complete terms.
            </p>";

        var htmlBody = Wrap(
            firstName: safeName,
            title: "Your seller account is approved",
            subtitle: "Welcome to ZansiHustle — let's get you selling",
            body: body,
            footerTitle: "ZansiHustle Seller Support");

        var plainTextBody = $@"Welcome to ZansiHustle, {who}.

Your seller account has been approved. You can now start listing products or offering services and receiving customer requests.

WHAT APPROVAL MEANS
- You're cleared to sell and list on ZansiHustle.
- Keep your listings, prices and availability accurate.
- Approval can be reviewed if there are repeated complaints, unsafe conduct, fraud, or policy breaches.

PRODUCT SELLING STANDARDS
- Only list products you actually have or can fulfil.
- Use accurate photos, prices, descriptions, sizes, condition and stock status.
- Never list broken, unsafe, counterfeit, stolen, illegal, misleading or restricted items.
- Describe second-hand condition clearly. Broken/misrepresented products may be returned or refunded.
- Repeated complaints can affect your listing visibility or account status.

ACCEPTING ORDERS
- Only accept an order when you can fulfil it.
- Don't accept if the item is unavailable/damaged or you can't prepare it for collection.
- Repeated rejections, missed pickups or unfulfilled orders may affect your seller trust and listing performance.
- Prepare parcels on time; have the parcel ready for scheduled courier pickup.

DELIVERY & PICKUP
- ZansiHustle may arrange delivery via its dispatch partners.
- Confirm your pickup address, package safely, don't miss pickups.
- Use the in-app flow; respect customers' private contact and address info.

SERVICE PROVIDER STANDARDS
- Only accept bookings you can honour; arrive on time or communicate early.
- Keep descriptions, pricing, fees, availability and visit options accurate.
- Respect safety and privacy; never request off-platform/unsafe arrangements.
- Mark services complete only when genuinely done.

SAFETY & CONDUCT
- Your safety and your customers' both matter; keep communication respectful.
- Report suspicious behaviour; don't share customer info beyond fulfilment needs.
- ZansiHustle may step in for disputes, safety reports, fraud or repeated complaints.

YOUR SHOP
- Your shop is your public storefront — brand, listings, area, credibility.
- A complete profile builds customer confidence.

HOW TO SUCCEED
- Keep listings updated, respond quickly, accept only what you can fulfil.
- Package properly, keep appointments professional, use clear photos, keep prices honest.
- Watch notifications; contact support when unsure.

SUPPORT
- Email: {support}

ZansiHustle may update its seller guidelines over time. Continued use means following current platform rules. This email is onboarding guidance, not a full legal contract — see the in-app and website policies for full terms.

ZansiHustle Seller Support";

        return (subject, htmlBody, plainTextBody);
    }

    /// <summary>
    /// Builds the internal notification for new lead (sent to team).
    /// </summary>
    public static (string Subject, string HtmlBody, string PlainTextBody) BuildNewLeadNotification(string leadName, string phone, string? email, string? category, string? province, string? referrerName, string? city = null)
    {
        var subject = $"🆕 New Seller Lead: {leadName}";

        var htmlBody = Wrap(
            firstName: "Team",  // Generic greeting for internal email
            title: "New Seller Lead Received",
            subtitle: "A new hustler has joined the waitlist",
            body: $@"
            <table style='width: 100%; border-collapse: collapse; margin: 20px 0; background: {Surface}; border-radius: 12px; overflow: hidden;'>
                <tr style='background: {Primary}; color: white;'>
                    <th style='padding: 12px; text-align: left;'>Field</th>
                    <th style='padding: 12px; text-align: left;'>Value</th>
                </tr>
                <tr style='border-bottom: 1px solid {Border};'>
                    <td style='padding: 12px; font-weight: bold;'>Name</td>
                    <td style='padding: 12px;'>{WebUtility.HtmlEncode(leadName)}</td>
                </tr>
                {(string.IsNullOrWhiteSpace(phone) ? "" : $@"
                <tr style='border-bottom: 1px solid {Border};'>
                    <td style='padding: 12px; font-weight: bold;'>Phone</td>
                    <td style='padding: 12px;'>{WebUtility.HtmlEncode(phone)}</td>
                </tr>")}
                {(string.IsNullOrWhiteSpace(email) ? "" : $@"
                <tr style='border-bottom: 1px solid {Border};'>
                    <td style='padding: 12px; font-weight: bold;'>Email</td>
                    <td style='padding: 12px;'>{WebUtility.HtmlEncode(email)}</td>
                </tr>")}
                {(string.IsNullOrWhiteSpace(category) ? "" : $@"
                <tr style='border-bottom: 1px solid {Border};'>
                    <td style='padding: 12px; font-weight: bold;'>Category</td>
                    <td style='padding: 12px;'>{WebUtility.HtmlEncode(category)}</td>
                </tr>")}
                {(string.IsNullOrWhiteSpace(province) ? "" : $@"
                <tr style='border-bottom: 1px solid {Border};'>
                    <td style='padding: 12px; font-weight: bold;'>Province</td>
                    <td style='padding: 12px;'>{WebUtility.HtmlEncode(province)}</td>
                </tr>")}
                {(string.IsNullOrWhiteSpace(city) ? "" : $@"
                <tr style='border-bottom: 1px solid {Border};'>
                    <td style='padding: 12px; font-weight: bold;'>City/Town</td>
                    <td style='padding: 12px;'>{WebUtility.HtmlEncode(city)}</td>
                </tr>")}
                {(string.IsNullOrWhiteSpace(referrerName) ? "" : $@"
                <tr style='border-bottom: 1px solid {Border};'>
                    <td style='padding: 12px; font-weight: bold;'>Referred By</td>
                    <td style='padding: 12px;'>{WebUtility.HtmlEncode(referrerName)}</td>
                </tr>")}
            </table>
            
            <p style='margin-top: 20px; color: {TextMuted}; font-size: 12px;'>This lead was automatically added to the system and marked as pending review.</p>",
            footerTitle: "ZansiHustle Operations Team",
            isInternalEmail: true);  // Internal email, no VIP badge, no product tagline

        var plainTextBody = $@"
            New Seller Lead Received: {leadName}

            Details:
            Name: {leadName}
            Phone: {phone}
            Email: {email ?? "Not provided"}
            Category: {category ?? "Not provided"}
            Province: {province ?? "Not provided"}
            City: {city ?? "Not provided"}
            Referrer: {referrerName ?? "None"}

            This lead was automatically added to the system and marked as pending review.

            ZansiHustle Operations Team";

        return (subject, htmlBody, plainTextBody);
    }

    private static string Wrap(string firstName, string title, string subtitle, string body, string footerTitle, bool showVipBadge = false, bool isInternalEmail = false)
    {
        var vipBadge = showVipBadge ? @"
                                    <div style='margin-top: 12px;'>
                                        <span style='background: linear-gradient(135deg, " + Gold + @" 0%, " + Success + @" 100%); color: white; font-size: 11px; font-weight: bold; padding: 4px 12px; border-radius: 20px;'>✨ VIP EARLY ACCESS ✨</span>
                                    </div>" : "";

        // For internal emails, we don't show the product tagline
        var productTagline = isInternalEmail ? "" : $@"
                                    <div style='margin-top:10px; font-size:13px; color:{TextMuted};'>Products • Services • Shops</div>";

        // For internal emails, change the greeting from "Hi {firstName}," to something appropriate
        var greeting = isInternalEmail ? "Hi Team," : $"Hi {WebUtility.HtmlEncode(firstName)},";

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>ZansiHustle</title>
            </head>
            <body style='margin:0; padding:0; background:{Surface2}; font-family:Arial, Helvetica, sans-serif; color:{Text};'>
                <table cellpadding='0' cellspacing='0' width='100%' style='background:{Surface2}; padding:32px 12px;'>
                    <tr>
                        <td align='center'>
                            <table cellpadding='0' cellspacing='0' width='640' style='max-width:640px; background:{Background}; border:1px solid {Border}; border-radius:18px; overflow:hidden;'>
                                <tr>
                                    <td style='background:{White}; text-align:center; padding:28px 20px 22px 20px; border-bottom:1px solid {Border};'>
                                        <div style='font-size:30px; line-height:1; font-family: ""Montserrat"", ""Canva Montserrat"", -apple-system, BlinkMacSystemFont, sans-serif;'>
                                            <span style='color:{Black}; font-weight:400;'>Zansi</span><span style='color:{Primary}; font-weight:800;'>Hustle</span>
                                        </div>
                                        {productTagline}
                                        {vipBadge}
                                    </td>
                                </tr>

                                <tr>
                                    <td style='padding:34px 28px;'>
                                        <p style='margin-top:0; margin-bottom:16px; font-size:16px; color:{Text};'>{greeting}</p>

                                        <h2 style='margin:0 0 6px 0; font-size:24px; line-height:1.3; color:{Text};'>{WebUtility.HtmlEncode(title)}</h2>
                                        <p style='margin:0 0 24px 0; font-size:14px; color:{TextMuted};'>{WebUtility.HtmlEncode(subtitle)}</p>

                                        <div style='font-size:15px; line-height:1.75; color:{Text};'>
                                            {body}
                                        </div>

                                        <p style='margin-top:32px; border-top:1px solid {Border}; padding-top:20px; line-height:1.6; color:{Text};'>
                                            <strong>{WebUtility.HtmlEncode(footerTitle)}</strong><br />
                                            <span style='color:{Black}; font-weight:400;'>Zansi</span><span style='color:{Primary}; font-weight:800;'>Hustle</span>
                                        </p>
                                    </td>
                                </tr>

                                <tr>
                                    <td style='background:{Surface}; text-align:center; padding:18px 20px; font-size:12px; color:{LightGray}; border-top:1px solid {Border};'>
                                        © {DateTime.UtcNow.Year} ZansiHustle. All rights reserved.
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";
    }

    private static string Safe(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "there" : value.Trim();
    }
}