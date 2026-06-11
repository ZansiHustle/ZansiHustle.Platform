namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// Provider's reason for rejecting a booking. <see cref="ReasonCode"/> is one
    /// of the allowed codes (NotAvailable / LocationTooFar / CannotProvideService
    /// / CustomerDetailsIncomplete / Emergency / Other). <see cref="ReasonText"/>
    /// is required (and must be a clear explanation) when the code is "Other".
    /// </summary>
    public class RejectBookingRequestDto
    {
        public string ReasonCode { get; set; } = string.Empty;
        public string? ReasonText { get; set; }
    }
}
