namespace ZansiHustle.Application.Merchants.Dtos
{
    /// <summary>
    /// Payload for the admin reject-merchant action. Reason is optional —
    /// surfaced in logs/audit; not currently persisted on the merchant row.
    /// </summary>
    public class RejectMerchantRequestDto
    {
        public string? Reason { get; set; }
    }
}
