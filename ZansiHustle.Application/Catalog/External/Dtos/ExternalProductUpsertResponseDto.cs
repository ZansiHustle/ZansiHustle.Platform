using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Catalog.External.Dtos
{
    public sealed class ExternalProductUpsertResponseDto
    {
        public Guid ListingId { get; set; }
        public string ExternalProductId { get; set; } = string.Empty;

        /// <summary>"Created" | "Updated" | "SkippedStale" — SkippedStale means sourceUpdatedAtUtc was not newer than what's already stored; nothing was changed.</summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>Maps externalVariantId → the internal ListingVariant id, for every variant in the request (created, updated, or already-existing).</summary>
        public Dictionary<string, Guid> VariantIds { get; set; } = new();
    }
}
