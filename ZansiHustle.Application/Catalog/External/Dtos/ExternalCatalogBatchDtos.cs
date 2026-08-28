using System;
using System.Collections.Generic;

namespace ZansiHustle.Application.Catalog.External.Dtos
{
    /// <summary>Response for POST /external-catalogs/{sourceCode}/sync-runs — starts a full-snapshot run.</summary>
    public sealed class ExternalCatalogSyncRunStartResponseDto
    {
        public Guid SyncRunId { get; set; }
    }

    /// <summary>Request for POST /external-catalogs/{sourceCode}/sync-runs/{syncRunId}/products/batch.</summary>
    public sealed class ExternalCatalogBatchUpsertRequestDto
    {
        public List<ExternalProductUpsertRequestDto> Products { get; set; } = new();
    }

    public sealed class ExternalCatalogBatchUpsertResponseDto
    {
        public int Succeeded { get; set; }
        public int Failed { get; set; }
        public List<ExternalCatalogBatchItemResultDto> Results { get; set; } = new();
    }

    public sealed class ExternalCatalogBatchItemResultDto
    {
        public string ExternalProductId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public Guid? ListingId { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>Request for POST /external-catalogs/{sourceCode}/sync-runs/{syncRunId}/fail.</summary>
    public sealed class ExternalCatalogSyncRunFailRequestDto
    {
        public string? ErrorMessage { get; set; }
    }

    public sealed class ExternalCatalogSyncRunCompleteResponseDto
    {
        public Guid SyncRunId { get; set; }
        public int ProductsUpserted { get; set; }
        public int ProductsArchived { get; set; }
    }

    /// <summary>Response for GET /external-catalogs/{sourceCode}/status.</summary>
    public sealed class ExternalCatalogStatusResponseDto
    {
        public bool Enabled { get; set; }
        public DateTime? LastActivityAtUtc { get; set; }
        public string? LastActivityType { get; set; }
        public bool LastActivitySucceeded { get; set; }
        public string? LastError { get; set; }
        public int TotalProductsUpserted { get; set; }
        public int TotalProductsArchived { get; set; }
        public DateTime? LastFullSyncCompletedAtUtc { get; set; }
    }
}
