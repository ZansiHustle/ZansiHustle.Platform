namespace ZansiHustle.Application.Payments.External.Dtos
{
    /// <summary>
    /// FROZEN wire contract — POST /external-payments/sessions. Property
    /// names must serialize to the exact camelCase keys ZansiTech already
    /// sends (shopCode, shopName, externalOrderId, ...). Do not rename.
    /// </summary>
    public sealed class CreateExternalPaymentSessionRequestDto
    {
        public string ShopCode { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string ExternalOrderId { get; set; } = string.Empty;
        public string ExternalOrderNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string? CustomerEmail { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
    }
}
