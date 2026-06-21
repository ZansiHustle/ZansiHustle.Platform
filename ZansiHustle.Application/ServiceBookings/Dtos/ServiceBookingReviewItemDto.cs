using System;

namespace ZansiHustle.Application.ServiceBookings.Dtos
{
    /// <summary>
    /// A single service-booking review (one direction). <see cref="Direction"/>
    /// is serialized as a stable snake_case string
    /// ("customer_to_provider" / "provider_to_customer") rather than the enum
    /// value so the client contract is human-readable and stable.
    /// </summary>
    public class ServiceBookingReviewItemDto
    {
        public Guid Id { get; set; }

        /// <summary>"customer_to_provider" or "provider_to_customer".</summary>
        public string Direction { get; set; } = string.Empty;

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
