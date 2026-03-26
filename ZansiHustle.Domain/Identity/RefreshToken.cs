namespace ZansiHustle.Domain.Identity;

/// <summary>
/// Represents a refresh token issued to a user.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
    public DateTime? RevokedOnUtc { get; set; }
}