using System.ComponentModel.DataAnnotations;
using ZansiHustle.Shared.Enums.User;

namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Registration request payload.
/// </summary>
public sealed class RegisterDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public List<UserRole> UserRoles { get; set; } = new List<UserRole> { UserRole.Customer };
}