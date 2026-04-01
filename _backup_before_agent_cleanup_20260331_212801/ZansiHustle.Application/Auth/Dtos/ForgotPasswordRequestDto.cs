using System.ComponentModel.DataAnnotations;

namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Forgot-password request payload.
/// </summary>
public sealed class ForgotPasswordRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}