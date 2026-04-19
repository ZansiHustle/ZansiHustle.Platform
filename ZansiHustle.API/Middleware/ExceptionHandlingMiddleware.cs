using System.Net;
using System.Text.Json;

namespace ZansiHustle.API.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns a consistent JSON response.
///
/// The response body is verbose (exception type + message + stack) when:
///   - the hosting environment is NOT Production, OR
///   - configuration flag "Diagnostics:ExposeExceptionDetails" is true
///
/// Both conditions are needed because UAT often runs with
/// ASPNETCORE_ENVIRONMENT=Production and we still need to diagnose it.
/// Production keeps the generic message so nothing internal leaks.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment env,
        IConfiguration config)
    {
        _next = next;
        _logger = logger;
        _env = env;
        _config = config;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.TraceIdentifier;
            _logger.LogError(ex,
                "Unhandled exception on {Method} {Path} (trace {TraceId})",
                context.Request.Method, context.Request.Path, correlationId);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var exposeDetails =
                !_env.IsProduction() ||
                string.Equals(_config["Diagnostics:ExposeExceptionDetails"], "true",
                    StringComparison.OrdinalIgnoreCase);

            object payload = exposeDetails
                ? new
                {
                    success = false,
                    code = "INTERNAL_SERVER_ERROR",
                    message = $"{ex.GetType().Name}: {ex.Message}",
                    innerMessage = ex.InnerException?.Message,
                    stack = ex.ToString(),
                    traceId = correlationId,
                }
                : new
                {
                    success = false,
                    code = "INTERNAL_SERVER_ERROR",
                    message = "An unexpected error occurred.",
                    traceId = correlationId,
                };

            var json = JsonSerializer.Serialize(payload);
            await context.Response.WriteAsync(json);
        }
    }
}
