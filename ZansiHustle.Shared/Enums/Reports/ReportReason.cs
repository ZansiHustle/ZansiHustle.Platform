namespace ZansiHustle.Shared.Enums.Reports
{
    /// <summary>
    /// Why a user reported a piece of content. Aligns with the Community
    /// Guidelines prohibited categories surfaced in the app's Report sheet.
    /// </summary>
    public enum ReportReason
    {
        Spam = 1,
        Fraud = 2,
        Counterfeit = 3,
        Harassment = 4,
        HateSpeech = 5,
        IllegalContent = 6,
        AdultContent = 7,
        Violence = 8,
        Other = 99
    }
}
