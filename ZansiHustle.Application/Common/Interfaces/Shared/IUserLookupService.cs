namespace ZansiHustle.Application.Common.Interfaces.Shared;

/// <summary>
/// Lookup of persisted user contact info by id. Sits in the Application
/// layer so services can read User fields (Email, PhoneNumber, names)
/// without pulling in ASP.NET Identity. Implementation in the API layer
/// wraps <c>UserManager&lt;User&gt;</c>.
/// </summary>
public interface IUserLookupService
{
    Task<UserContactInfo?> GetContactAsync(Guid userId);
}

/// <summary>
/// Minimal projection of a User record — enough for shop-profile fallback
/// when merchant fields are null. Kept as a record so it's easy to add
/// more fields later (e.g. province/city from UserProfile) without
/// breaking callers.
/// </summary>
public sealed record UserContactInfo(
    string? Email,
    string? PhoneNumber,
    string? FirstName,
    string? LastName);
