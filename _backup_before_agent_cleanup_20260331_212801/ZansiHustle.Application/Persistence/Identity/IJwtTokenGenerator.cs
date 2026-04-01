using ZansiHustle.Application.Auth.Dtos;
using ZansiHustle.Domain.Identity;

namespace ZansiHustle.Application.Persistence.Identity;

/// <summary>
/// Generates, refreshes, and revokes JWT access and refresh tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates access and refresh tokens for a user.
    /// </summary>
    Task<AuthTokenDto> GenerateTokenAsync(User user);

    /// <summary>
    /// Refreshes a token pair using a valid refresh token.
    /// </summary>
    Task<AuthTokenDto?> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes a specific refresh token.
    /// </summary>
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Revokes all active refresh tokens for a user.
    /// </summary>
    Task RevokeAllRefreshTokensForUserAsync(Guid userId);
}