namespace ZansiHustle.Application.Engagement.Dtos
{
    /// <summary>
    /// Uniform response shape for every like / follow / save mutation.
    /// Tells the client two things:
    ///
    ///   • <see cref="Active"/> — whether the engagement is currently
    ///     on (i.e. row exists) AFTER the call. <c>true</c> after a
    ///     successful POST, <c>false</c> after a successful DELETE.
    ///     Lets the client trust this value to flip its local toggle
    ///     without a follow-up GET.
    ///
    ///   • <see cref="Count"/> — the parent entity's denormalised
    ///     count AFTER the call, so cards can update their badge
    ///     without re-fetching the listing/shop/store.
    /// </summary>
    public sealed class EngagementToggleResultDto
    {
        public bool Active { get; set; }
        public int Count { get; set; }
    }
}
