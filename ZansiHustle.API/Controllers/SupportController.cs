// ZansiHustle.API/Controllers/SupportController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Application.Support;
using ZansiHustle.Application.Support.Dtos;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers;

/// <summary>
/// Exposes endpoints for customer support and contact forms.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/[controller]")]
public class SupportController : BaseController
{
    private readonly ISupportEmailService _supportEmailService;

    public SupportController(ISupportEmailService supportEmailService)
    {
        _supportEmailService = supportEmailService;
    }

    /// <summary>
    /// Sends a contact message to support and sends a confirmation to the user.
    /// </summary>
    [HttpPost("contact")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    public async Task<IActionResult> Contact([FromBody] ContactRequestDto request)
    {
        var result = await _supportEmailService.ProcessContactFormAsync(request);
        return ToActionResult(result);
    }
}