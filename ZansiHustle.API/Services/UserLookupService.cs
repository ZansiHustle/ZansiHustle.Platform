using Microsoft.AspNetCore.Identity;
using ZansiHustle.Application.Common.Interfaces.Shared;
using ZansiHustle.Domain.Identity;

namespace ZansiHustle.API.Services;

/// <summary>
/// Wraps <see cref="UserManager{User}"/> so Application-layer services can
/// look up persisted User contact info without referencing ASP.NET Identity.
/// </summary>
public sealed class UserLookupService : IUserLookupService
{
    private readonly UserManager<User> _userManager;

    public UserLookupService(UserManager<User> userManager) => _userManager = userManager;

    public async Task<UserContactInfo?> GetContactAsync(Guid userId)
    {
        var u = await _userManager.FindByIdAsync(userId.ToString());
        if (u is null) return null;
        return new UserContactInfo(u.Email, u.PhoneNumber, u.FirstName, u.LastName);
    }
}
