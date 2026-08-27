namespace ZansiHustle.Application.Payments.External.Dtos
{
    /// <summary>
    /// FROZEN wire contract — body of GET /external-payments/sessions/{sessionId}.
    /// Status is one of: Pending, RedirectCreated, Processing, Paid, Failed,
    /// Cancelled, Expired (see ExternalPaymentSessionStatus).
    /// </summary>
    public sealed class ExternalPaymentSessionStatusResponseDto
    {
        public string Status { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
    }
}
