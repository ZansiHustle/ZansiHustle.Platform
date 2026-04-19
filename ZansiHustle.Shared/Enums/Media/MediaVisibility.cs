namespace ZansiHustle.Shared.Enums.Media
{
    public enum MediaVisibility
    {
        /// <summary>Stored in the private container, only resolvable via signed URL.</summary>
        Private = 1,

        /// <summary>Stored in the public container, served directly via CDN URL.</summary>
        Public = 2,

        /// <summary>Stored in the private container but signed URLs are anonymous-friendly (e.g. shareable preview links).</summary>
        Signed = 3,
    }
}
