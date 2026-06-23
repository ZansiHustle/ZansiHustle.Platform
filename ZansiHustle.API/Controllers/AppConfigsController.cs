using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.AppConfigs;

namespace ZansiHustle.API.Controllers
{
    /// <summary>
    /// Public, anonymous read of remote app configs the mobile app gates on.
    /// Only rows flagged <c>IsPublic</c> are returned.
    /// </summary>
    [Route("api/app-configs")]
    public class AppConfigsController : BaseController
    {
        private readonly IAppRuntimeConfigService _service;

        public AppConfigsController(IAppRuntimeConfigService service)
        {
            _service = service;
        }

        /// <summary>key → { enabled, title, message } map for the mobile app.</summary>
        [HttpGet("public")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublic()
        {
            var result = await _service.GetPublicAsync();
            return ToActionResult(result);
        }
    }
}
