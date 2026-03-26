using System.ComponentModel.DataAnnotations;

namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Email verification confirmation request payload.
/// </summary>
public sealed class VerifyEmailRequestDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;
}