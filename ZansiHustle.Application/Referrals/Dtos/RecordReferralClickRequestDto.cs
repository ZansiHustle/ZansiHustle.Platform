namespace ZansiHustle.Application.Referrals.Dtos
{
    public class RecordReferralClickRequestDto
    {
        public string ReferralCode { get; set; } = string.Empty;
        public string? LandingPath { get; set; }
        public string? UserAgent { get; set; }
    }
}
