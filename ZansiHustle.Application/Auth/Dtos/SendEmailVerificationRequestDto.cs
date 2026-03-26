using System.ComponentModel.DataAnnotations;

namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Email verification send request payload.
/// </summary>
public sealed class SendEmailVerificationRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}