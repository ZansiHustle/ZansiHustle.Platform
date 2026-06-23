namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Represents current authenticated user information.
/// </summary>
public sealed class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
    public string AccountStatus { get; set; } = string.Empty;

    /// <summary>The user's profile picture URL (from their UserProfile), if set.</summary>
    public string? ProfileImageUrl { get; set; }
}