using System;
using ZansiHustle.Shared.Enums.ZansiDispatch;

namespace ZansiHustle.Domain.ZansiDispatch
{
    /// <summary>
    /// Request/response log for provider calls. In Phase 1 the InternalEstimate
    /// "call" is logged for symmetry; the real value lands later when
    /// CourierGuy/Shiplogic HTTP calls need debugging. Payloads are stored as
    /// JSON strings (callers must scrub secrets before logging). UTC timestamp.
    /// </summary>
    public class ZansiDispatchProviderRequestLog
    {
        public Guid Id { get; set; }

        public ZansiDispatchProviderType ProviderType { get; set; }
        public ZansiDispatchProviderOperation Operation { get; set; }

        public string? RequestJson { get; set; }
        public string? ResponseJson { get; set; }

        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        /// <summary>HTTP status code of the provider call (null for the internal path).</summary>
        public int? StatusCode { get; set; }
        public int? DurationMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
