namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Represents issued authentication tokens.
/// </summary>
public sealed class AuthTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public Guid UserId { get; set; }
    public CurrentUserDto User { get; set; } = new();
}