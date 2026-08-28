using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Catalog.External;
using ZansiHustle.Application.Catalog.External.Dtos;
using ZansiHustle.Shared.Errors;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Server-to-server External Catalog Sync API. NOT a public browser API
    /// — every endpoint requires the X-ZansiHustle-Catalog-Secret header
    /// issued to a registered source. Uses a SEPARATE secret namespace from
    /// External Payments — the two features never share a credential even
    /// for the same external shop (e.g. ZansiTech has one secret for
    /// payments and a different one for catalog sync).
    ///
    /// Routed WITHOUT the "api/" prefix, same rationale as
    /// ExternalPaymentsController: this codebase has no global /api prefix
    /// mechanism, and keeping this off /api means it's automatically exempt
    /// from the mobile hard-update gate (correct — no caller here is the
    /// ZansiHustle mobile app).
    ///
    /// Response shape is raw JSON, NOT the {success,message,data} envelope
    /// BaseController uses for ZansiHustle's own clients — this is an
    /// integration surface with its own contract, so this inherits
    /// ControllerBase directly (mirrors ExternalPaymentsController).
    /// </summary>
    [ApiController]
    [Route("external-catalogs")]
    public class ExternalCatalogsController : ControllerBase
    {
        private const string CatalogSecretHeader = "X-ZansiHustle-Catalog-Secret";

        private readonly IExternalCatalogSyncService _service;

        public ExternalCatalogsController(IExternalCatalogSyncService service)
        {
            _service = service;
        }

        /// <summary>POST /external-catalogs/{sourceCode}/products/upsert — idempotent create-or-update of one product + its variants.</summary>
        [HttpPost("{sourceCode}/products/upsert")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExternalProductUpsertResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertProduct(string sourceCode, [FromBody] ExternalProductUpsertRequestDto? request, CancellationToken cancellationToken)
        {
            var result = await _service.UpsertProductAsync(sourceCode, ReadCatalogSecret(), request, syncRunId: null, cancellationToken);
            return FromResult(result);
        }

        /// <summary>POST /external-catalogs/{sourceCode}/products/{externalProductId}/archive — soft-archives a mirrored listing. Never hard-deletes; historical orders are untouched.</summary>
        [HttpPost("{sourceCode}/products/{externalProductId}/archive")]
        [AllowAnonymous]
        public async Task<IActionResult> ArchiveProduct(string sourceCode, string externalProductId, CancellationToken cancellationToken)
        {
            var result = await _service.ArchiveProductAsync(sourceCode, ReadCatalogSecret(), externalProductId, cancellationToken);
            return FromResult(result);
        }

        /// <summary>POST /external-catalogs/{sourceCode}/sync-runs — starts a full-snapshot run for the manual "sync all products" / drift-repair flow.</summary>
        [HttpPost("{sourceCode}/sync-runs")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExternalCatalogSyncRunStartResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartSyncRun(string sourceCode, CancellationToken cancellationToken)
        {
            var result = await _service.StartFullSyncRunAsync(sourceCode, ReadCatalogSecret(), cancellationToken);
            return FromResult(result);
        }

        /// <summary>POST /external-catalogs/{sourceCode}/sync-runs/{syncRunId}/products/batch — upserts a batch of products within an in-progress run.</summary>
        [HttpPost("{sourceCode}/sync-runs/{syncRunId:guid}/products/batch")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExternalCatalogBatchUpsertResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertBatch(string sourceCode, Guid syncRunId, [FromBody] ExternalCatalogBatchUpsertRequestDto? request, CancellationToken cancellationToken)
        {
            var result = await _service.UpsertBatchAsync(sourceCode, ReadCatalogSecret(), syncRunId, request, cancellationToken);
            return FromResult(result);
        }

        /// <summary>
        /// POST /external-catalogs/{sourceCode}/sync-runs/{syncRunId}/complete — the archive
        /// boundary. Only a successful call here archives Listings for this source that
        /// weren't touched by this run; a run that's never completed never archives anything.
        /// </summary>
        [HttpPost("{sourceCode}/sync-runs/{syncRunId:guid}/complete")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExternalCatalogSyncRunCompleteResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CompleteSyncRun(string sourceCode, Guid syncRunId, CancellationToken cancellationToken)
        {
            var result = await _service.CompleteFullSyncRunAsync(sourceCode, ReadCatalogSecret(), syncRunId, cancellationToken);
            return FromResult(result);
        }

        /// <summary>POST /external-catalogs/{sourceCode}/sync-runs/{syncRunId}/fail — marks a run failed. Never archives.</summary>
        [HttpPost("{sourceCode}/sync-runs/{syncRunId:guid}/fail")]
        [AllowAnonymous]
        public async Task<IActionResult> FailSyncRun(string sourceCode, Guid syncRunId, [FromBody] ExternalCatalogSyncRunFailRequestDto? request, CancellationToken cancellationToken)
        {
            var result = await _service.FailFullSyncRunAsync(sourceCode, ReadCatalogSecret(), syncRunId, request?.ErrorMessage, cancellationToken);
            return FromResult(result);
        }

        /// <summary>GET /external-catalogs/{sourceCode}/status — last sync time/count/failure/enabled, for drift diagnosis.</summary>
        [HttpGet("{sourceCode}/status")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ExternalCatalogStatusResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatus(string sourceCode, CancellationToken cancellationToken)
        {
            var result = await _service.GetStatusAsync(sourceCode, ReadCatalogSecret(), cancellationToken);
            return FromResult(result);
        }

        private string? ReadCatalogSecret() =>
            Request.Headers.TryGetValue(CatalogSecretHeader, out var value) ? value.ToString() : null;

        private IActionResult FromResult(Result result)
        {
            if (result.IsSuccess)
                return Ok(new { message = result.Message });

            return StatusCode(MapStatus(result.Code), new { error = result.Code, message = result.Message });
        }

        private IActionResult FromResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
                return Ok(result.Data);

            return StatusCode(MapStatus(result.Code), new { error = result.Code, message = result.Message });
        }

        private static int MapStatus(string code) => code switch
        {
            ErrorCodes.BadRequest => StatusCodes.Status400BadRequest,
            ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
            ErrorCodes.NotFound => StatusCodes.Status404NotFound,
            ErrorCodes.Conflict => StatusCodes.Status409Conflict,
            ErrorCodes.ProviderNotConfigured => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
