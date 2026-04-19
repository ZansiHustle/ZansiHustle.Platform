namespace ZansiHustle.Shared.Enums.Media
{
    /// <summary>
    /// Lifecycle of a media asset. Pending → Uploaded happens when the client
    /// finalises the upload. Verification-class assets then enter admin
    /// review (PendingReview → Approved | Rejected). Public listing media
    /// skips review entirely and lands at NotApplicable.
    /// </summary>
    public enum MediaStatus
    {
        /// <summary>Row created, awaiting blob upload.</summary>
        Pending = 1,

        /// <summary>Blob uploaded; not (yet) in admin review.</summary>
        Uploaded = 2,

        /// <summary>Awaiting admin verification review.</summary>
        PendingReview = 3,

        /// <summary>Admin approved the asset (verification-class only).</summary>
        Approved = 4,

        /// <summary>Admin rejected the asset (verification-class only).</summary>
        Rejected = 5,

        /// <summary>Display-only media that doesn't need review.</summary>
        NotApplicable = 6,
    }
}
