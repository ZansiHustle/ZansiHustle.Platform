using System;

namespace ZansiHustle.Domain.Marketplace
{
    /// <summary>
    /// One image attached to a <see cref="MarketplaceListing"/>.
    ///
    /// Stores the public URL of an asset that the client previously
    /// uploaded via the existing media-upload pipeline (R2-backed
    /// <see cref="ZansiHustle.Application.Media.IMediaService"/>). This
    /// table is intentionally light — it does NOT duplicate the
    /// MediaAsset metadata, just the rendered URL the buyer-side cards
    /// consume.
    /// </summary>
    public class MarketplaceListingImage
    {
        public Guid Id { get; set; }

        public Guid ListingId { get; set; }
        public MarketplaceListing? Listing { get; set; }

        /// <summary>Public URL fetched after the client finalises the upload.</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>0-based ordering for the image gallery.</summary>
        public int SortOrder { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
