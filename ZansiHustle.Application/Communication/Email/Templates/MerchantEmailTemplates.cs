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