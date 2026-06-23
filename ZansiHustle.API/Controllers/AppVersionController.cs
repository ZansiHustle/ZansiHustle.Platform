using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.AppVersion;
using ZansiHustle.Application.AppVersion.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Public, anonymous mobile version-check endpoint. The mobile app calls this
    /// at launch (and the portal previews it) to decide whether to show a soft /
    /// hard update prompt. Always returns a populated answer (DB rule → generic →
    /// appsettings fallback); force-update is never implied by missing data.
    /// </summary>
    [Route("api/app-version")]
    [AllowAnonymous]
    public class AppVersionController : BaseController
    {
        private readonly IMobileAppVersionService _service;

        public AppVersionController(IMobileAppVersionService service)
        {
            _service = service;
        }

        /// <summary>
        /// GET /api/app-version/mobile?platform=&amp;channel=&amp;version=&amp;buildNumber=
        /// Lowercase platform/channel; missing values default to android/google.
        /// </summary>
        [HttpGet("mobile")]
        public async Task<IActionResult> CheckMobile(
            [FromQuery] string? platform,
            [FromQuery] string? channel,
            [FromQuery] string? version,
            [FromQuery] string? buildNumber,
            CancellationToken cancellationToken)
        {
            var parsedPlatform = MobileAppWireParser.ParsePlatform(platform);
            var parsedChannel = MobileAppWireParser.ParseChannel(channel);
            var parsedBuild = MobileAppWireParser.ParseBuildNumber(buildNumber);

            var response = await _service.CheckAsync(
                parsedPlatform, parsedChannel, version, parsedBuild, cancellationToken);

            return ToActionResult(Result<MobileAppVersionCheckResponseDto>.Success(response));
        }
    }
}
