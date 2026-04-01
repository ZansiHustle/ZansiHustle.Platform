namespace ZansiHustle.Domain.Identity;

/// <summary>
/// Represents application settings and preferences for a user.
/// </summary>
public class UserSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool PushNotificationsEnabled { get; set; } = true;
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOnUtc { get; set; }
}