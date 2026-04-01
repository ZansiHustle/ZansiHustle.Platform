using Microsoft.AspNetCore.Identity;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Shared.Enums.User;

namespace ZansiHustle.Infrastructure.Data;

/// <summary>
/// Seeds default identity roles.
/// </summary>
public static class IdentitySeeder
{
    /// <summary>
    /// Ensures required platform roles exist.
    /// </summary>
    public static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        foreach (var role in Enum.GetNames<UserRole>())
        {
            var exists = await roleManager.RoleExistsAsync(role);
            if (!exists)
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    NormalizedName = role.ToUpperInvariant()
                });
            }
        }
    }
}