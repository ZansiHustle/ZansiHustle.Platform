using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Communications.Email.Models;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Communications.Email.Interfaces;

/// <summary>
/// Sends the post-booking "shipment booked" emails (customer + seller). Honours
/// the <see cref="EmailTestModeSettings"/> recipient override so QA can verify
/// both on a single inbox. Best-effort — failures never break the booking flow.
/// </summary>
public interface IShipmentEmailService
{
    /// <summary>Send BOTH the customer and seller shipment-booked emails.</summary>
    Task SendShipmentBookedEmailsAsync(ShipmentEmailContext context, CancellationToken cancellationToken = default);
}
