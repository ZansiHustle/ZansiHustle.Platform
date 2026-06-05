namespace ZansiHustle.Shared.Enums.Chat
{
    /// <summary>
    /// What domain object the conversation is anchored to. The shape
    /// is a discriminated union — at most one of the FK columns
    /// (MarketplaceListingId / OrderId / …) on Conversation is set,
    /// and this enum says which one is meaningful.
    ///
    /// MarketplaceListing — buyer ↔ seller talking about a casual
    /// resale listing BEFORE any purchase. Created or fetched by the
    /// "Message seller" button on the listing detail screen.
    ///
    /// Order — buyer ↔ merchant talking about a placed/paid order.
    /// Implemented at the schema level so the start-order-conversation
    /// endpoint can ship alongside marketplace without a follow-up
    /// migration.
    ///
    /// Direct / Support are reserved values — not implemented in v1.
    /// </summary>
    public enum ConversationType
    {
        MarketplaceListing = 1,
        Order              = 2,
        Direct             = 3,
        Support            = 4,
    }
}
