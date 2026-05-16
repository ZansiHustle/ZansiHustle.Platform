using System.ComponentModel.DataAnnotations;

namespace ZansiHustle.Application.Auth.Dtos;

/// <summary>
/// Request payload for <c>POST /api/auth/check-availability</c>. Both
/// fields are optional individually, but at least one of <c>Email</c> or
/// <c>PhoneNumber</c> must be supplied — the server returns
/// <see cref="ErrorCodes.BadRequest"/> otherwise.
///
/// Used by the registration wizard's Step&nbsp;1 ("Contact") so the user
/// finds out about a duplicate before they invest in filling the rest
/// of the form. No account is created here — this is a pure read.
/// </summary>
public sealed class CheckAvailabilityRequestDto
{
    [EmailAddress]
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Response payload for <c>POST /api/auth/check-availability</c>. Each
/// flag mirrors the corresponding request field: if the caller omitted
/// the field, the flag is <c>true</c> (nothing to conflict with). When
/// both fields are sent, both flags reflect real lookups.
/// </summary>
public sealed class CheckAvailabilityResponseDto
{
    public bool EmailAvailable { get; set; }
    public bool PhoneAvailable { get; set; }
}
