namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>
    /// Body for the seller "reject product order" endpoint. A reason is required —
    /// see <see cref="OrderService.RejectAsync"/>.
    /// </summary>
    public class RejectOrderRequestDto
    {
        public string? Reason { get; set; }
    }
}
