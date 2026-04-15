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
        var status = code switch
        {
            "BAD_REQUEST" => StatusCodes.Status400BadRequest,
            "WEAK_PASSWORD" => StatusCodes.Status400BadRequest,
            "OTP_INVALID" => StatusCodes.Status400BadRequest,
            "OTP_EXPIRED" => StatusCodes.Status400BadRequest,
            "OTP_EXHAUSTED" => StatusCodes.Status400BadRequest,
            "INVALID_RESET_TOKEN" => StatusCodes.Status400BadRequest,

            "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
            "INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
            "INVALID_REFRESH_TOKEN" => StatusCodes.Status401Unauthorized,
            "REFRESH_TOKEN_EXPIRED" => StatusCodes.Status401Unauthorized,

            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "INACTIVE_ACCOUNT" => StatusCodes.Status403Forbidden,
            "EMAIL_NOT_CONFIRMED" => StatusCodes.Status403Forbidden,

            "NOT_FOUND" => StatusCodes.Status404NotFound,

            "CONFLICT" => StatusCodes.Status409Conflict,
            "EMAIL_TAKEN" => StatusCodes.Status409Conflict,

            "TOO_MANY_REQUESTS" => StatusCodes.Status429TooManyRequests,
            "OTP_RESEND_COOLDOWN" => StatusCodes.Status429TooManyRequests,

            "EMAIL_SEND_FAILED" => StatusCodes.Status502BadGateway,
            "SMS_SEND_FAILED" => StatusCodes.Status502BadGateway,
            "WHATSAPP_SEND_FAILED" => StatusCodes.Status502BadGateway,
            "PAYMENT_INIT_FAILED" => StatusCodes.Status502BadGateway,
            "PROVIDER_NOT_CONFIGURED" => StatusCodes.Status503ServiceUnavailable,
            "PAYMENT_NOT_ALLOWED" => StatusCodes.Status400BadRequest,
            "PAYMENT_ALREADY_PAID" => StatusCodes.Status409Conflict,
            "PAYMENT_AMOUNT_MISMATCH" => StatusCodes.Status409Conflict,
            "WEBHOOK_SIGNATURE_INVALID" => StatusCodes.Status400BadRequest,

            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(status, new { success = false, code, message });
    }
}