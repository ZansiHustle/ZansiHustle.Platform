namespace ZansiHustle.Application.Media.Dtos
{
    public class ReviewMediaRequestDto
    {
        /// <summary>true → Approved, false → Rejected.</summary>
        public bool Approved { get; set; }
        public string? RejectionReason { get; set; }
    }
}
