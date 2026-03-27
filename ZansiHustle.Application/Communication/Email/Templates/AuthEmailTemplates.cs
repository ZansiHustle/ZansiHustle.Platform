using System.Net;

namespace ZansiHustle.Application.Communications.Email.Templates;

/// <summary>
/// Builds ZansiHustle authentication email templates.
/// </summary>
public static class AuthEmailTemplates
{
    private const string Background = "#FFFFFF";
    private const string Surface = "#F8FAFC";
    private const string Surface2 = "#F1F5F9";
    private const string Primary = "#16A34A";
    private const string PrimarySoft = "#22C55E";
    private const string Text = "#111827";
    private const string TextMuted = "#6B7280";
    private const string Border = "#E5E7EB";
    private const string Success = "#16A34A";
    private const string Danger = "#EF4444";
    private const string Warning = "#F59E0B";
    private const string White = "#FFFFFF";
    private const string Dark = "#0F172A";
    private const string Black = "#0F172A";
    private const string LightGray = "#58595A";

    public static (string Subject, string HtmlBody, string PlainTextBody) BuildVerifyEmail(string firstName, string verificationLink)
    {
        var safeName = Safe(firstName);

        var subject = "Verify your ZansiHustle email";
        var htmlBody = Wrap(
            firstName: safeName,
            title: "Verify Your Email Address",
            subtitle: "Welcome to ZansiHustle",
            body: $@"
                <p>Thanks for signing up. Please verify your email address to activate your account and continue using ZansiHustle.</p>

                <div style='margin:28px 0; text-align:center;'>
                    <a href='{verificationLink}'
                       style='display:inline-block; background:{Primary}; color:{White}; text-decoration:none; padding:14px 24px; border-radius:10px; font-weight:700;'>
                        Verify Email
                    </a>
                </div>

                <p>If the button above does not work, copy and paste this link into your browser:</p>
                <p style='word-break:break-word; color:{TextMuted}; background:{Surface}; border:1px solid {Border}; padding:14px 16px; border-radius:10px;'>{WebUtility.HtmlEncode(verificationLink)}</p>

                <p style='margin-top:24px;'>If you did not create this account, you can safely ignore this email.</p>",
            footerTitle: "ZansiHustle Security Team");

        var plainText = $"""
            Hi {safeName},

            Thanks for signing up for ZansiHustle.

            Please verify your email address by visiting the link below:
            {verificationLink}

            If you did not create this account, you can safely ignore this email.

            ZansiHustle Security Team
            """;

        return (subject, htmlBody, plainText);
    }

    public static (string Subject, string HtmlBody, string PlainTextBody) BuildEmailVerifiedConfirmation(string firstName)
    {
        var safeName = Safe(firstName);

        var subject = "Your ZansiHustle email is verified";
        var htmlBody = Wrap(
            firstName: safeName,
            title: "Email Verified Successfully",
            subtitle: "Your account is now active",
            body: $@"
                <p>Your email address has been verified successfully.</p>
                <p>You can now log in and continue using ZansiHustle to discover products, services, and shops.</p>
                <div style='margin-top:24px; padding:14px 16px; background:{Surface}; border:1px solid {Border}; border-left:4px solid {Success}; border-radius:10px;'>
                    <strong>Next step:</strong> open the app and sign in.
                </div>",
            footerTitle: "ZansiHustle Team");

        var plainText = $"""
            Hi {safeName},

            Your email address has been verified successfully.

            You can now sign in and continue using ZansiHustle.

            ZansiHustle Team
            """;

        return (subject, htmlBody, plainText);
    }

    public static (string Subject, string HtmlBody, string PlainTextBody) BuildOtpEmail(string firstName, string otpCode)
    {
        var safeName = Safe(firstName);
        var safeOtp = Safe(otpCode);

        var subject = "Your ZansiHustle OTP code";
        var htmlBody = Wrap(
            firstName: safeName,
            title: "Your One-Time Password",
            subtitle: "Use this code to continue",
            body: $@"
                <p>Use the code below to continue your ZansiHustle action.</p>

                <div style='margin:28px 0; text-align:center;'>
                    <div style='display:inline-block; letter-spacing:8px; font-size:30px; font-weight:800; color:{Dark}; background:{Surface}; border:1px solid {Border}; padding:16px 24px; border-radius:12px;'>
                        {WebUtility.HtmlEncode(safeOtp)}
                    </div>
                </div>

                <p>This code should only be used by you. Do not share it with anyone.</p>
                <div style='margin-top:20px; padding:14px 16px; background:{Surface}; border:1px solid {Border}; border-left:4px solid {Warning}; border-radius:10px; color:{Text};'>
                    If you did not request this code, please secure your account immediately.
                </div>",
            footerTitle: "ZansiHustle Security Team");

        var plainText = $"""
            Hi {safeName},

            Your ZansiHustle OTP code is:

            {safeOtp}

            Do not share this code with anyone.

            ZansiHustle Security Team
            """;

        return (subject, htmlBody, plainText);
    }

    public static (string Subject, string HtmlBody, string PlainTextBody) BuildPasswordResetEmail(string firstName, string resetLink)
    {
        var safeName = Safe(firstName);

        var subject = "Reset your ZansiHustle password";
        var htmlBody = Wrap(
            firstName: safeName,
            title: "Password Reset Request",
            subtitle: "Reset your password securely",
            body: $@"
                <p>We received a request to reset your password.</p>

                <div style='margin:28px 0; text-align:center;'>
                    <a href='{resetLink}'
                       style='display:inline-block; background:{Primary}; color:{White}; text-decoration:none; padding:14px 24px; border-radius:10px; font-weight:700;'>
                        Reset Password
                    </a>
                </div>

                <p>If the button above does not work, copy and paste this link into your browser:</p>
                <p style='word-break:break-word; color:{TextMuted}; background:{Surface}; border:1px solid {Border}; padding:14px 16px; border-radius:10px;'>{WebUtility.HtmlEncode(resetLink)}</p>

                <div style='margin-top:20px; padding:14px 16px; background:{Surface}; border:1px solid {Border}; border-left:4px solid {Danger}; border-radius:10px; color:{Text};'>
                    If you did not request a password reset, you can ignore this email.
                </div>",
            footerTitle: "ZansiHustle Security Team");

        var plainText = $"""
            Hi {safeName},

            We received a request to reset your password.

            Reset your password using the link below:
            {resetLink}

            If you did not request this, ignore this email.

            ZansiHustle Security Team
            """;

        return (subject, htmlBody, plainText);
    }

    private static string Wrap(string firstName, string title, string subtitle, string body, string footerTitle)
    {
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
                                        <div style='font-size:30px; line-height:1;font-family: ""Montserrat"", ""Canva Montserrat"", -apple-system, BlinkMacSystemFont, sans-serif;'>
                                            <span style='color:{Black}; font-weight:400;'>Zansi</span><span style='color:{Primary}; font-weight:800;'>Hustle</span>
                                        </div>
                                        <div style='margin-top:10px; font-size:13px; color:{TextMuted};'>Products • Services • Shops</div>
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