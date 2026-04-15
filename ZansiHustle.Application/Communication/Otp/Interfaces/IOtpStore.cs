using ZansiHustle.Shared.Enums.Communications;

namespace ZansiHustle.Application.Communications.Otp.Interfaces;

/// <summary>
/// Persistence contract for OTP sessions. Implementations may be in-memory,
/// Redis, or SQL-backed. For v1 we ship an in-memory implementation; swap to
/// a distributed store when scaling horizontally.
/// </summary>
public interface IOtpStore
{
    Task SaveAsync(OtpSessionRecord record, CancellationToken cancellationToken = default);
    Task<OtpSessionRecord?> FindAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<OtpSessionRecord?> FindActiveByDestinationAsync(
        string destination,
        OtpPurpose purpose,
        OtpChannel channel,
        CancellationToken cancellationToken = default);
    Task UpdateAsync(OtpSessionRecord record, CancellationToken cancellationToken = default);
    Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Server-side record of an issued OTP session. The <see cref="CodeHash"/>
/// is stored instead of the raw code so a store compromise does not leak
/// plaintext OTPs.
/// </summary>
public sealed class OtpSessionRecord
{
    public string SessionId { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public OtpChannel Channel { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public bool IsConsumed { get; set; }
    public Guid? UserId { get; set; }
}
