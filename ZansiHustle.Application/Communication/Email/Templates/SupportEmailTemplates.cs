// ZansiHustle.Application/Communications/Email/Templates/SupportEmailTemplates.cs
using System.Net;
using ZansiHustle.Application.Support.Dtos;

namespace ZansiHustle.Application.Communications.Email.Templates;

/// <summary>
/// Builds ZansiHustle support email templates.
/// </summary>
public static class SupportEmailTemplates
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

    /// <summary>
    /// Builds the email sent to support team when someone submits contact form.
    /// </summary>
    public static (string Subject, string HtmlBody, string PlainTextBody) BuildSupportNotification(ContactRequestDto request)
    {
        var subject = $"[Contact Form] {request.Subject ?? "General Inquiry"} from {request.Name}";

        var htmlBody = Wrap(
            firstName: "Support Team",
            title: "New Contact Form Submission",
            subtitle: "A new message has been received from the website",
            body: $@"
                <p><strong>From:</strong> {WebUtility.HtmlEncode(request.Name)} ({WebUtility.HtmlEncode(request.Email)})</p>
                <p><strong>Subject:</strong> {WebUtility.HtmlEncode(request.Subject ?? "General Inquiry")}</p>
                
                <div style='background-color: {Surface}; border-left: 4px solid {Primary}; padding: 16px; margin: 20px 0; border-radius: 8px;'>
                    <p style='margin: 0 0 8px 0;'><strong>Message:</strong></p>
                    <p style='margin: 0; color: {TextMuted}; white-space: pre-wrap;'>{WebUtility.HtmlEncode(request.Message)}</p>
                </div>
                
                <p style='margin-top: 20px;'>You can reply directly to this email to respond to the customer.</p>",
            footerTitle: "ZansiHustle Support Team",
            isSupportEmail: true);

        var plainTextBody = $@"
            New Contact Form Submission
            =========================
            From: {request.Name} ({request.Email})
            Subject: {request.Subject ?? "General Inquiry"}
            Message: {request.Message}

            Submitted from ZansiHustle website contact form.

            You can reply directly to {request.Email} to respond to the customer.";

        return (subject, htmlBody, plainTextBody);
    }

    /// <summary>
    /// Builds the confirmation email sent to the user who submitted the contact form.
    /// </summary>
    public static (string Subject, string HtmlBody, string PlainTextBody) BuildUserConfirmation(ContactRequestDto request)
    {
        var safeName = Safe(request.Name);
        var subject = "We've received your message - ZansiHustle";

        var htmlBody = Wrap(
            firstName: safeName,
            title: "We've received your message",
            subtitle: "Thank you for contacting ZansiHustle",
            body: $@"
                <p>Thank you for reaching out to ZansiHustle. We've received your message and our team will respond within 24 hours.</p>
                
                <div style='background-color: {Surface}; border-left: 4px solid {Success}; padding: 16px; margin: 20px 0; border-radius: 8px;'>
                    <p style='margin: 0 0 8px 0;'><strong>Your message:</strong></p>
                    <p style='margin: 0; color: {TextMuted};'>{WebUtility.HtmlEncode(request.Message)}</p>
                </div>
                
                <p>In the meantime, you can:</p>
                <ul>
                    <li>Check our <a href='https://zansihustle.co.za/faq' style='color: {Primary};'>FAQ page</a> for quick answers</li>
                    <li>Follow us on social media for updates</li>
                    <li>Join our waitlist for early access</li>
                </ul>",
            footerTitle: "ZansiHustle Support Team");

        var plainTextBody = $@"
            Hello {request.Name},

            Thank you for reaching out to ZansiHustle. We've received your message and our team will respond within 24 hours.

            Your message:
            {request.Message}

            In the meantime, you can check our FAQ page or follow us on social media for updates.

            ZansiHustle — Products • Services • Shops
            support@zansihustle.co.za";

        return (subject, htmlBody, plainTextBody);
    }

    private static string Wrap(string firstName, string title, string subtitle, string body, string footerTitle, bool isSupportEmail = false)
    {
        // For support email, show customer info in the header area
        var headerContent = isSupportEmail ? "" : $@"
                                        <div style='margin-top:10px; font-size:13px; color:{TextMuted};'>Products • Services • Shops</div>";

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
                                        {headerContent}
                                    </td>
                                </tr>

                                <tr>
                                    <td style='padding:34px 28px;'>
                                        <p style='margin-top:0; margin-bottom:16px; font-size:16px; color:{Text};'>Hi {WebUtility.HtmlEncode(firstName)},</p>

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