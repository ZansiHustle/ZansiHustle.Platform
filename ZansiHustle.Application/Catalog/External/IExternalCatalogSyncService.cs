using System;
using System.Threading;
using System.Threading.Tasks;
using ZansiHustle.Application.Catalog.External.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.Application.Catalog.External
{
    public interface IExternalCatalogSyncService
    {
        /// <summary>Idempotent create-or-update of one product + its variants. Pass syncRunId when called as part of a full-snapshot batch, null for a standalone incremental upsert.</summary>
        Task<Result<ExternalProductUpsertResponseDto>> UpsertProductAsync(
            string sourceCode, string? providedSecret, ExternalProductUpsertRequestDto? request, Guid? syncRunId, CancellationToken cancellationToken = default);

        Task<Result> ArchiveProductAsync(string sourceCode, string? providedSecret, string externalProductId, CancellationToken cancellationToken = default);

        Task<Result<ExternalCatalogSyncRunStartResponseDto>> StartFullSyncRunAsync(string sourceCode, string? providedSecret, CancellationToken cancellationToken = default);

        Task<Result<ExternalCatalogBatchUpsertResponseDto>> UpsertBatchAsync(
            string sourceCode, string? providedSecret, Guid syncRunId, ExternalCatalogBatchUpsertRequestDto? request, CancellationToken cancellationToken = default);

        /// <summary>The archive boundary: archives every Listing for this source NOT touched by syncRunId. Only reachable on a successful, explicit completion.</summary>
        Task<Result<ExternalCatalogSyncRunCompleteResponseDto>> CompleteFullSyncRunAsync(string sourceCode, string? providedSecret, Guid syncRunId, CancellationToken cancellationToken = default);

        /// <summary>Marks a run Failed. Never archives anything.</summary>
        Task<Result> FailFullSyncRunAsync(string sourceCode, string? providedSecret, Guid syncRunId, string? errorMessage, CancellationToken cancellationToken = default);

        Task<Result<ExternalCatalogStatusResponseDto>> GetStatusAsync(string sourceCode, string? providedSecret, CancellationToken cancellationToken = default);
    }
}
