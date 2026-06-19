namespace ZansiHustle.Application.Listings.Dtos
{
    /// <summary>
    /// Outcome of attaching listings to a shop. Idempotent: items already in the
    /// shop are counted (not re-attached, not errored); items that aren't the
    /// caller's / don't belong to the shop's merchant are skipped.
    /// </summary>
    public class AssignShopItemsResultDto
    {
        /// <summary>Newly attached to the shop.</summary>
        public int Attached { get; set; }
        /// <summary>Were already in this shop (no-op).</summary>
        public int AlreadyInShop { get; set; }
        /// <summary>Skipped (not found / not owned / different merchant).</summary>
        public int Skipped { get; set; }
    }
}
