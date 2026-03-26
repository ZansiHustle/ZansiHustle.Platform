using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ZansiHustle.Application.Auth.Dtos;
using ZansiHustle.Application.Persistence.Identity;
using ZansiHustle.Domain.Identity;
using ZansiHustle.Infrastructure.Configuration;
using ZansiHustle.Infrastructure.Data;
using ZansiHustle.Shared.Enums.User;

namespace ZansiHustle.Infrastructure.Identity;

/// <summary>
/// Generates and manages JWT and refresh tokens.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly AppDbContext _dbContext;
    private readonly UserManager<User> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly SymmetricSecurityKey _securityKey;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenGenerator(
        AppDbContext dbContext,
        UserManager<User> userManager,
        IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;

        if (string.IsNullOrWhiteSpace(_jwtSettings.Key))
            throw new InvalidOperationException("JWT key is not configured.");

        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        _signingCredentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256);
    }

    /// <inheritdoc />
    public async Task<AuthTokenDto> GenerateTokenAsync(User user)
    {
        var claims = await BuildClaimsAsync(user);
        var accessToken = GenerateJwtToken(claims);
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthTokenDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            UserId = user.Id,
            User = new CurrentUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                AccountStatus = user.AccountStatus.ToString()
            }
        };
    }

    /// <inheritdoc />
    public async Task<AuthTokenDto?> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken && !x.IsRevoked);

        if (storedToken == null)
            return null;

        if (storedToken.ExpiresOnUtc <= DateTime.UtcNow)
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedOnUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            return null;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == storedToken.UserId);
        if (user == null || !user.IsActive || user.AccountStatus != AccountStatus.Active)
            return null;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            storedToken.IsRevoked = true;
            storedToken.RevokedOnUtc = DateTime.UtcNow;

            var newTokens = await GenerateTokenAsync(user);

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return newTokens;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken && !x.IsRevoked);

        if (storedToken == null)
            return false;

        storedToken.IsRevoked = true;
        storedToken.RevokedOnUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task RevokeAllRefreshTokensForUserAsync(Guid userId)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedOnUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task<List<Claim>> BuildClaimsAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("user_id", user.Id.ToString()),
            new("account_status", user.AccountStatus.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("platform_role", role));
        }

        return claims;
    }

    private string GenerateJwtToken(IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = GenerateSecureToken(),
            UserId = userId,
            CreatedOnUtc = DateTime.UtcNow,
            ExpiresOnUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync();

        return refreshToken.Token;
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes)
            .Replace("/", "_")
            .Replace("+", "-")
            .Replace("=", "");
    }
}