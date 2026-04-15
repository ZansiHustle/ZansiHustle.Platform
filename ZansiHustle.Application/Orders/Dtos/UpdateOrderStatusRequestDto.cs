using ZansiHustle.Shared.Enums.Orders;

namespace ZansiHustle.Application.Orders.Dtos
{
    /// <summary>
    /// Status-change request. Valid transitions depend on the caller's role
    /// (buyer vs seller) and the current status — see
    /// <see cref="OrderService.UpdateStatusAsync"/>.
    /// </summary>
    public class UpdateOrderStatusRequestDto
    {
        public OrderStatus Status { get; set; }
        public string? CancellationReason { get; set; }
    }
}
