namespace ZansiHustle.Shared.Errors;

/// <summary>
/// Common application error codes returned in the Result envelope.
/// Clients may branch on these codes for targeted error UX.
/// </summary>
public static class ErrorCodes
{
    // Generic buckets (map to HTTP status in BaseController.MapFailure)
    public const string BadRequest = "BAD_REQUEST";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string Exception = "EXCEPTION";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";

    // Auth specifics
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string WeakPassword = "WEAK_PASSWORD";
    public const string InactiveAccount = "INACTIVE_ACCOUNT";
    public const string EmailNotConfirmed = "EMAIL_NOT_CONFIRMED";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string RefreshTokenExpired = "REFRESH_TOKEN_EXPIRED";
    public const string InvalidResetToken = "INVALID_RESET_TOKEN";

    // OTP specifics
    public const string OtpInvalid = "OTP_INVALID";
    public const string OtpExpired = "OTP_EXPIRED";
    public const string OtpExhausted = "OTP_EXHAUSTED";
    public const string OtpResendCooldown = "OTP_RESEND_COOLDOWN";

    // Communications specifics
    public const string EmailSendFailed = "EMAIL_SEND_FAILED";
    public const string SmsSendFailed = "SMS_SEND_FAILED";
    public const string WhatsAppSendFailed = "WHATSAPP_SEND_FAILED";
    public const string ProviderNotConfigured = "PROVIDER_NOT_CONFIGURED";
    public const string PhoneVerificationFailed = "PHONE_VERIFICATION_FAILED";
    public const string InvalidPhoneNumber = "INVALID_PHONE_NUMBER";

    // Payment specifics
    public const string PaymentNotAllowed = "PAYMENT_NOT_ALLOWED";
    public const string PaymentInitFailed = "PAYMENT_INIT_FAILED";
    public const string PaymentAlreadyPaid = "PAYMENT_ALREADY_PAID";
    public const string PaymentAmountMismatch = "PAYMENT_AMOUNT_MISMATCH";
    public const string WebhookSignatureInvalid = "WEBHOOK_SIGNATURE_INVALID";

    /// <summary>
    /// The configured payment provider (Ozow / Yoco / Paystack) was reachable
    /// but returned a non-success response, or its HTTP call threw before a
    /// status came back. Distinct from <see cref="PaymentInitFailed"/> in
    /// intent: <c>PaymentInitFailed</c> is "the provider explicitly declined"
    /// while <c>PaymentProviderUnavailable</c> is "we couldn't get a usable
    /// answer right now — retry later". Mapped to HTTP 422 so Cloudflare
    /// (which intercepts 5xx origin responses and replaces them with its own
    /// branded error page) lets the structured envelope through to the
    /// client unchanged.
    /// </summary>
    public const string PaymentProviderUnavailable = "PAYMENT_PROVIDER_UNAVAILABLE";
}
