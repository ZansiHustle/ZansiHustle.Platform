namespace ZansiHustle.Application.Account.Dtos
{
    /// <summary>
    /// Body for <c>DELETE /api/account</c>. All fields optional — the caller is
    /// already identified by their bearer token; the client also enforces a
    /// "type DELETE to confirm" gate, so no server-side confirmation string is
    /// required. <see cref="Reason"/> is captured for churn analytics only.
    /// </summary>
    public class DeleteAccountRequestDto
    {
        /// <summary>Optional free-text reason the user gave for leaving.</summary>
        public string? Reason { get; set; }
    }
}
