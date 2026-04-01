using System.ComponentModel.DataAnnotations;

namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Refresh token request payload.
/// </summary>
public sealed class RefreshTokenRequestDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}