using Microsoft.AspNetCore.Mvc;
using ZansiHustle.Shared.Results;

namespace ZansiHustle.API.Controllers;

/// <summary>
/// Base API controller with helpers for converting service-layer results into HTTP responses.
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }

        return MapFailure(result.Code, result.Message);
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        return MapFailure(result.Code, result.Message);
    }

    private IActionResult MapFailure(string code, string message)
    {
        return code switch
        {
            "BAD_REQUEST" => BadRequest(new { success = false, code, message }),
            "UNAUTHORIZED" => Unauthorized(new { success = false, code, message }),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, new { success = false, code, message }),
            "NOT_FOUND" => NotFound(new { success = false, code, message }),
            "CONFLICT" => Conflict(new { success = false, code, message }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { success = false, code, message })
        };
    }
}