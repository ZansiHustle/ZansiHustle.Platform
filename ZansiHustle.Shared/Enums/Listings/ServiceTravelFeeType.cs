namespace ZansiHustle.Shared.Enums.Listings
{
    /// <summary>
    /// Travel-fee model for house-call services. Applies only when the service
    /// allows house calls; otherwise it is <see cref="None"/>.
    /// </summary>
    public enum ServiceTravelFeeType
    {
        /// <summary>No travel fee — the provider absorbs travel.</summary>
        None = 0,

        /// <summary>A single flat travel fee regardless of distance.</summary>
        FlatFee = 1,

        /// <summary>Per-kilometre travel fee based on road distance.</summary>
        PerKilometre = 2
    }
}
