using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Communications.Otp.Interfaces;
using ZansiHustle.Shared.Enums.Communications;

namespace ZansiHustle.Infrastructure.Communications.Otp;

/// <summary>
/// In-memory OTP session store. Fine for single-instance deployments.
/// For horizontally-scaled or load-balanced deployments replace with a
/// Redis/SQL-backed implementation — the <see cref="IOtpStore"/> surface
/// is deliberately minimal to make that swap easy.
/// </summary>
public sealed class InMemoryOtpStore : IOtpStore
{
    private readonly ConcurrentDictionary<string, OtpSessionRecord> _sessions = new();
    private readonly ILogger<InMemoryOtpStore> _logger;

    public InMemoryOtpStore(ILogger<InMemoryOtpStore> logger)
    {
        _logger = logger;
    }

    public Task SaveAsync(OtpSessionRecord record, CancellationToken cancellationToken = default)
    {
        _sessions[record.SessionId] = record;
        PurgeExpired();
        return Task.CompletedTask;
    }

    public Task<OtpSessionRecord?> FindAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(sessionId, out var record);
        return Task.FromResult<OtpSessionRecord?>(record);
    }

    public Task<OtpSessionRecord?> FindActiveByDestinationAsync(
        string destination,
        OtpPurpose purpose,
        OtpChannel channel,
        CancellationToken cancellationToken = default)
    {
        var match = _sessions.Values
            .Where(s => !s.IsConsumed
                        && s.ExpiresAtUtc > DateTime.UtcNow
                        && s.Purpose == purpose
                        && s.Channel == channel
                        && string.Equals(s.Destination, destination, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.CreatedOnUtc)
            .FirstOrDefault();
        return Task.FromResult<OtpSessionRecord?>(match);
    }

    public Task UpdateAsync(OtpSessionRecord record, CancellationToken cancellationToken = default)
    {
        _sessions[record.SessionId] = record;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }

    private void PurgeExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kvp in _sessions)
        {
            // Keep expired records for 1h for audit/log context, then drop.
            if (kvp.Value.ExpiresAtUtc < now.AddHours(-1))
            {
                _sessions.TryRemove(kvp.Key, out _);
            }
        }
    }
}
