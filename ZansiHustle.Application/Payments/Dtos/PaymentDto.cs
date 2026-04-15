using System;
using ZansiHustle.Shared.Enums.Payments;

namespace ZansiHustle.Application.Payments.Dtos
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public string? OrderCode { get; set; }
        public Guid UserId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string? ProviderReference { get; set; }
        public string? ProviderAuthorizationUrl { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public PaymentTransactionStatus Status { get; set; }
        public DateTime? PaidAtUtc { get; set; }
        public DateTime? FailedAtUtc { get; set; }
        public DateTime? RefundedAtUtc { get; set; }
        public string? FailureReason { get; set; }
        public string? ChannelUsed { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
