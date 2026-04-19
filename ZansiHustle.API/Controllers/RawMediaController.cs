using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Media.Storage;
using ZansiHustle.API.Storage;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Signed-URL passthrough for the LocalFilesystemMediaStorageService.
    /// In production with the Azure Blob adapter, the signed URL points
    /// directly at blob.core.windows.net and this controller is unused.
    ///
    /// PUT writes the request body to disk; GET streams the file back.
    /// Both verbs require the URL signature issued by IMediaStorageService.
    /// </summary>
    [AllowAnonymous]
    [Route("api/media/raw")]
    public class RawMediaController : ControllerBase
    {
        private readonly IMediaStorageService _storage;

        public RawMediaController(IMediaStorageService storage)
        {
            _storage = storage;
        }

        [HttpPut("{container}/{key}")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Put(string container, string key,
            [FromQuery] string sig, [FromQuery] long exp, [FromQuery] string verb = "PUT")
        {
            if (_storage is not LocalFilesystemMediaStorageService local)
                return NotFound(); // Not applicable to non-local adapters.

            if (!local.VerifySignature(container, key, "PUT", sig, exp))
                return Unauthorized(new { message = "Invalid or expired upload signature." });

            var path = local.GetAbsolutePath(container, key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            await using var fs = System.IO.File.Create(path);
            await Request.Body.CopyToAsync(fs);

            return NoContent();
        }

        [HttpGet("{container}/{key}")]
        public async Task<IActionResult> Get(string container, string key,
            [FromQuery] string sig, [FromQuery] long exp, [FromQuery] string verb = "GET")
        {
            if (_storage is not LocalFilesystemMediaStorageService local)
                return NotFound();

            if (!local.VerifySignature(container, key, "GET", sig, exp))
                return Unauthorized(new { message = "Invalid or expired read signature." });

            var path = local.GetAbsolutePath(container, key);
            if (!System.IO.File.Exists(path)) return NotFound();

            // Best-effort content-type from extension; the client typically
            // already knows from the metadata DTO.
            var contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".mov" => "video/quicktime",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream",
            };

            var stream = System.IO.File.OpenRead(path);
            await Task.CompletedTask;
            return File(stream, contentType);
        }
    }
}
