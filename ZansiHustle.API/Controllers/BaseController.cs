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
        // ── HTTP status mapping ──────────────────────────────────────────────
        //
        // Cloudflare (and most CDN edges) intercept origin 5xx responses and
        // replace the body with their own branded error page — even when the
        // origin's 5xx carries a structured JSON envelope. The buyer then
        // sees "Cloudflare 502 origin_bad_gateway" instead of our actual
        // user-facing failure message, and the mobile error layer reads
        // CF's HTML, not our `{ success: false, code, message }`.
        //
        // To keep the structured envelope reaching clients, ANY failure that
        // is an APPLICATION/UPSTREAM-PROVIDER condition (not a real origin
        // outage) is mapped to a 4xx — typically 422 Unprocessable Entity.
        // Cloudflare passes 4xx through unchanged. The mobile / Postman /
        // Swagger client gets the real envelope and the engineer sees the
        // real reason.
        //
        // Genuine origin-was-broken outcomes (uncaught exception, missing
        // route) still surface as 5xx via the framework defaults / the
        // ExceptionHandlingMiddleware — those should be rare and visible.
        var status = code switch
        {
            "BAD_REQUEST" => StatusCodes.Status400BadRequest,
            "WEAK_PASSWORD" => StatusCodes.Status400BadRequest,
            "OTP_INVALID" => StatusCodes.Status400BadRequest,
            "OTP_EXPIRED" => StatusCodes.Status400BadRequest,
            "OTP_EXHAUSTED" => StatusCodes.Status400BadRequest,
            "INVALID_RESET_TOKEN" => StatusCodes.Status400BadRequest,
            "INVALID_PHONE_NUMBER" => StatusCodes.Status400BadRequest,

            "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
            "INVALID_CREDENTIALS" => StatusCodes.Status401Unauthorized,
            "INVALID_REFRESH_TOKEN" => StatusCodes.Status401Unauthorized,
            "REFRESH_TOKEN_EXPIRED" => StatusCodes.Status401Unauthorized,

            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "INACTIVE_ACCOUNT" => StatusCodes.Status403Forbidden,
            "EMAIL_NOT_CONFIRMED" => StatusCodes.Status403Forbidden,
            // Remote App-Control gate closed this flow (e.g. checkout/payment
            // paused from the Portal). A clean business 403, not a 500.
            "FEATURE_DISABLED" => StatusCodes.Status403Forbidden,

            "NOT_FOUND" => StatusCodes.Status404NotFound,

            "CONFLICT" => StatusCodes.Status409Conflict,
            "EMAIL_TAKEN" => StatusCodes.Status409Conflict,

            "TOO_MANY_REQUESTS" => StatusCodes.Status429TooManyRequests,
            "OTP_RESEND_COOLDOWN" => StatusCodes.Status429TooManyRequests,

            // ── Upstream-provider failures (was 502/503 → now 422) ───────────
            // Origin processed the request fine; an upstream third-party
            // declined or was unreachable. NOT an origin fault. 422 keeps
            // Cloudflare from substituting its own 502/503 page.
            "EMAIL_SEND_FAILED" => StatusCodes.Status422UnprocessableEntity,
            "SMS_SEND_FAILED" => StatusCodes.Status422UnprocessableEntity,
            "WHATSAPP_SEND_FAILED" => StatusCodes.Status422UnprocessableEntity,
            "PHONE_VERIFICATION_FAILED" => StatusCodes.Status422UnprocessableEntity,
            "PAYMENT_INIT_FAILED" => StatusCodes.Status422UnprocessableEntity,
            "PAYMENT_PROVIDER_UNAVAILABLE" => StatusCodes.Status422UnprocessableEntity,
            "PROVIDER_NOT_CONFIGURED" => StatusCodes.Status422UnprocessableEntity,

            "PAYMENT_NOT_ALLOWED" => StatusCodes.Status400BadRequest,
            "PAYMENT_ALREADY_PAID" => StatusCodes.Status409Conflict,
            "PAYMENT_AMOUNT_MISMATCH" => StatusCodes.Status409Conflict,
            "WEBHOOK_SIGNATURE_INVALID" => StatusCodes.Status400BadRequest,

            // Genuine origin-was-broken bucket. Anything that ends up here
            // (= EXCEPTION + unknown code) is a real bug + will be visible
            // as a CF-substituted 502 page on the client — and that's the
            // right signal.
            _ => StatusCodes.Status500InternalServerError
        };

        return StatusCode(status, new { success = false, code, message });
    }
}