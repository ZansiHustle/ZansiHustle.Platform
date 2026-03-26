namespace ZansiHustle.Domain.Identity;

/// <summary>
/// Represents extended profile information for a user.
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Bio { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOnUtc { get; set; }
}