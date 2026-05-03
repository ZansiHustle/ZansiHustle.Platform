using System.Text.Json.Serialization;

namespace ZansiHustle.Application.Payments.Providers
{
    // Low-level Ozow request/response contracts. JSON property names match the
    // casing Ozow expects on the wire (PascalCase for JSON, application/json
    // request bodies; the token endpoint uses form-urlencoded — fields there
    // are sent manually, not by the JSON serializer).

    // ─── Token ───────────────────────────────────────────────────────────────

    /// <summary>Form-urlencoded token request payload (sent as form fields, not JSON).</summary>
    public sealed class OzowTokenRequest
    {
        public string SiteCode { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string GrantType { get; set; } = "client_credentials";
    }

    public sealed class OzowTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    // ─── Create payment request ──────────────────────────────────────────────

    /// <summary>
    /// Body for <c>POST /PostPaymentRequest</c>. All fields participate in the
    /// SHA512 hashCheck — see <see cref="IOzowHashService"/>.
    /// </summary>
    public sealed class OzowPaymentRequest
    {
        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; } = "ZA";

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("transactionReference")]
        public string TransactionReference { get; set; } = string.Empty;

        [JsonPropertyName("bankReference")]
        public string BankReference { get; set; } = string.Empty;

        [JsonPropertyName("optional1")]
        public string? Optional1 { get; set; }

        [JsonPropertyName("optional2")]
        public string? Optional2 { get; set; }

        [JsonPropertyName("optional3")]
        public string? Optional3 { get; set; }

        [JsonPropertyName("optional4")]
        public string? Optional4 { get; set; }

        [JsonPropertyName("optional5")]
        public string? Optional5 { get; set; }

        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; } = "ZAR";

        [JsonPropertyName("siteCode")]
        public string SiteCode { get; set; } = string.Empty;

        [JsonPropertyName("isTest")]
        public bool IsTest { get; set; }

        [JsonPropertyName("successUrl")]
        public string SuccessUrl { get; set; } = string.Empty;

        [JsonPropertyName("cancelUrl")]
        public string CancelUrl { get; set; } = string.Empty;

        [JsonPropertyName("errorUrl")]
        public string ErrorUrl { get; set; } = string.Empty;

        [JsonPropertyName("notifyUrl")]
        public string NotifyUrl { get; set; } = string.Empty;

        /// <summary>SHA512 hex string computed by <see cref="IOzowHashService.GenerateRequestHash"/>.</summary>
        [JsonPropertyName("hashCheck")]
        public string HashCheck { get; set; } = string.Empty;

        [JsonPropertyName("customer")]
        public string? Customer { get; set; }

        [JsonPropertyName("bankReferenceCustomer")]
        public string? BankReferenceCustomer { get; set; }
    }

    /// <summary>
    /// Result of <c>POST /PostPaymentRequest</c>. Ozow returns a
    /// <c>paymentRequestId</c> and a <c>url</c> — the user is redirected
    /// to the URL to complete the EFT.
    /// </summary>
    public sealed class OzowPaymentRequestResult
    {
        [JsonPropertyName("paymentRequestId")]
        public string? PaymentRequestId { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }
    }

    // ─── Webhook / NotifyUrl ─────────────────────────────────────────────────

    /// <summary>
    /// Server-to-server payload Ozow POSTs to <c>NotifyUrl</c> after a
    /// transaction reaches a terminal state. Sent as
    /// <c>application/x-www-form-urlencoded</c> — controller binds via
    /// <see cref="Microsoft.AspNetCore.Mvc.FromFormAttribute"/>.
    /// All fields participate in the response hash — see
    /// <see cref="IOzowHashService.ValidateNotificationHash"/>.
    /// </summary>
    public sealed class OzowTransactionNotification
    {
        public string? SiteCode { get; set; }
        public string? TransactionId { get; set; }
        public string? TransactionReference { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Complete | Cancelled | Error | Abandoned | PendingInvestigation | Pending</summary>
        public string? Status { get; set; }
        public string? Optional1 { get; set; }
        public string? Optional2 { get; set; }
        public string? Optional3 { get; set; }
        public string? Optional4 { get; set; }
        public string? Optional5 { get; set; }
        public string? CurrencyCode { get; set; }
        public bool IsTest { get; set; }
        public string? StatusMessage { get; set; }
        public string? Hash { get; set; }
        public string? SubStatus { get; set; }
        public string? MaskedAccountNumber { get; set; }
        public string? BankName { get; set; }
        public string? SmartIndicators { get; set; }
    }

    // ─── Verify (GET /GetTransactionByReference, GET /GetTransaction) ───────

    /// <summary>
    /// Shape returned by Ozow's transaction-lookup endpoints. Includes the
    /// canonical Status field needed for server-side reconciliation.
    /// </summary>
    public sealed class OzowTransactionModel
    {
        [JsonPropertyName("transactionId")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("merchantCode")]
        public string? MerchantCode { get; set; }

        [JsonPropertyName("siteCode")]
        public string? SiteCode { get; set; }

        [JsonPropertyName("transactionReference")]
        public string? TransactionReference { get; set; }

        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>Complete | Cancelled | Error | Abandoned | PendingInvestigation | Pending</summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("statusMessage")]
        public string? StatusMessage { get; set; }

        [JsonPropertyName("subStatus")]
        public string? SubStatus { get; set; }

        [JsonPropertyName("createdDate")]
        public string? CreatedDate { get; set; }

        [JsonPropertyName("paymentDate")]
        public string? PaymentDate { get; set; }

        [JsonPropertyName("amountSettled")]
        public decimal? AmountSettled { get; set; }

        [JsonPropertyName("bankName")]
        public string? BankName { get; set; }

        [JsonPropertyName("maskedAccountNumber")]
        public string? MaskedAccountNumber { get; set; }
    }
}
