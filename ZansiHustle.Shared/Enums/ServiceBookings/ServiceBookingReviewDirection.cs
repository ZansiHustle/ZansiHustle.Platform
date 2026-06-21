namespace ZansiHustle.Shared.Enums.ServiceBookings;

/// <summary>
/// Direction of a two-way service-booking review. A completed booking can carry
/// at most one Active review per direction: the customer rating the provider,
/// and the provider rating the customer.
/// </summary>
public enum ServiceBookingReviewDirection
{
    /// <summary>The customer reviewing the provider. Reviewee = provider owner user.</summary>
    CustomerToProvider = 1,

    /// <summary>The provider reviewing the customer. Reviewee = booking customer.</summary>
    ProviderToCustomer = 2,
}
