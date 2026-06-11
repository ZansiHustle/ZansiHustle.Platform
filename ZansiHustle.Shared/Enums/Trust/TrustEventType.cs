namespace ZansiHustle.Shared.Enums.Trust;

/// <summary>
/// Kind of trust-signal event. Recorded as raw history NOW (no scoring, no
/// penalties, nothing shown to users) so a future scoring layer / ZansiPulse
/// can derive seller and customer trust from real behaviour. Integer-backed.
/// </summary>
public enum TrustEventType
{
    // Seller signals
    BookingRejected = 1,
    BookingAccepted = 2,
    BookingCompleted = 3,
    LateCancellation = 4,

    // Customer signals
    BookingCancelled = 100,
    BookingRejectedReceived = 101,
    CompletedBooking = 102
}
