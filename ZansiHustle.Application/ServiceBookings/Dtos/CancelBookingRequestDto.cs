namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// Customer's reason for cancelling a booking (V1: allowed only while the
    /// booking is still Requested / awaiting the provider). <see cref="ReasonCode"/>
    /// is one of the allowed codes (BookedByMistake / WrongDateTime / NoLongerNeeded
    /// / FoundAnotherProvider / ProviderTakingTooLong / Other). <see cref="ReasonText"/>
    /// is required (min 10 chars) when the code is "Other".
    /// </summary>
    public class CancelBookingRequestDto
    {
        public string ReasonCode { get; set; } = string.Empty;
        public string? ReasonText { get; set; }
    }
}
