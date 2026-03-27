namespace ZansiHustle.Application.Common.Interfaces.Shared;

/// <summary>
/// Provides access to the current authenticated user context.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the authenticated user's unique identifier, if available.
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the authenticated user's email address, if available.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Indicates whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }
}