namespace ZansiHustle.Application.Marketplace.Dtos
{
    /// <summary>
    /// Body for <c>POST /api/marketplace-listings/{id}/images</c>.
    ///
    /// The actual binary upload is NOT handled by this endpoint — it
    /// goes through the existing R2-backed media-upload pipeline
    /// (<see cref="ZansiHustle.Application.Media.IMediaService"/>).
    /// The client uploads the file, gets a public URL, and posts that
    /// URL here to attach it to the listing.
    /// </summary>
    public class AddMarketplaceListingImageRequestDto
    {
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Optional ordering hint. When omitted the new image is
        /// appended at the end of the listing's image list.
        /// </summary>
        public int? SortOrder { get; set; }
    }
}
