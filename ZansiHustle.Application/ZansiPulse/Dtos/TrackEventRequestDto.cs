using System;

namespace ZansiHustle.Application.ZansiPulse.Dtos
{
    /// <summary>
    /// Payload for <c>POST /api/zansipulse/events/track</c>. Every field
    /// except <see cref="EventType"/> is optional — supply whatever context
    /// the action carries. <see cref="EventType"/> is the case-insensitive
    /// name of a <c>ZansiPulseEventType</c> (e.g. "ViewListing").
    /// </summary>
    public class TrackEventRequestDto
    {
        public string EventType { get; set; } = string.Empty;

        public Guid? ListingId { get; set; }
        public Guid? SellerId { get; set; }
        public Guid? ShopId { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }

        public string? SearchTerm { get; set; }
        public string? Province { get; set; }
        public string? City { get; set; }
        public decimal? Price { get; set; }

        /// <summary>
        /// For search events — how many results the search returned. When
        /// supplied it feeds <c>ResultCount</c> / <c>NoResultCount</c> on the
        /// search-term metric (0 results = unmet-demand signal).
        /// </summary>
        public int? ResultCount { get; set; }

        /// <summary>Optional free-form JSON context stored verbatim on the event.</summary>
        public string? MetadataJson { get; set; }
    }
}
