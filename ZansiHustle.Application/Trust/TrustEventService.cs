using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZansiHustle.Application.Persistence.Trust;
using ZansiHustle.Domain.Trust;
using ZansiHustle.Shared.Enums.Trust;

namespace ZansiHustle.Application.Trust
{
    /// <inheritdoc />
    public sealed class TrustEventService : ITrustEventService
    {
        private readonly ITrustEventRepository _repository;
        private readonly ILogger<TrustEventService> _logger;

        public TrustEventService(ITrustEventRepository repository, ILogger<TrustEventService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task RecordAsync(
            Guid userId,
            TrustActorRole actorRole,
            TrustEventType type,
            string referenceType,
            Guid referenceId,
            object? metadata = null)
        {
            if (userId == Guid.Empty) return;
            try
            {
                await _repository.AddAsync(new TrustEvent
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ActorRole = actorRole,
                    Type = type,
                    ReferenceType = referenceType,
                    ReferenceId = referenceId == Guid.Empty ? null : referenceId,
                    ScoreImpact = null,
                    MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
                    CreatedAtUtc = DateTime.UtcNow
                });
                await _repository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Best-effort: trust telemetry must never break the operation.
                _logger.LogError(ex,
                    "[Trust] Failed to record {Type} for user {UserId} ({RefType}:{RefId}).",
                    type, userId, referenceType, referenceId);
            }
        }
    }
}
