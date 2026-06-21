namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// Request body for creating (POST) or editing (PUT) the viewer's own
    /// service-booking review. The direction + reviewee are resolved
    /// server-side from the booking + caller, never trusted from the client.
    /// </summary>
    public class SubmitServiceBookingReviewRequestDto
    {
        /// <summary>1..5.</summary>
        public int Rating { get; set; }

        /// <summary>Optional free-text comment (max 1000 chars).</summary>
        public string? Comment { get; set; }
    }
}
