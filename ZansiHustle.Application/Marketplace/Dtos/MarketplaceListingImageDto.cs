using System;

namespace ZansiHustle.Application.Marketplace.Dtos
{
    /// <summary>
    /// One image attached to a marketplace listing — surfaced on the
    /// detail DTO as <c>ImageItems</c> so the owner-edit screen can
    /// reference each image by id (needed for the DELETE endpoint).
    ///
    /// The flat <c>Images: List&lt;string&gt;</c> field on
    /// <see cref="MarketplaceListingDto"/> is preserved for buyer-side
    /// surfaces (cards, gallery) that only need URLs and don't need
    /// per-image management.
    /// </summary>
    public class MarketplaceListingImageDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public int SortOrder { get; set; }

        // TODO (image variants — phase 2): expose `string? ThumbnailUrl`
        // once the upload pipeline emits a small variant (≈320px) on
        // finalize. Card / list surfaces should render the thumbnail;
        // the detail gallery keeps using `Url`. The frontend
        // marketplaceMapper already has the mirroring TODO.
    }
}
