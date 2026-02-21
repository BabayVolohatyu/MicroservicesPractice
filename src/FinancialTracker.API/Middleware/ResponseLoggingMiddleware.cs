using System.Diagnostics;

namespace FinancialTracker.API.Middleware;

/// <summary>
/// Logs every HTTP response with status code, method, path, duration and optional reason.
/// Controllers can set the reason via: HttpContext.Items[ResponseLoggingMiddleware.ResponseReasonKey] = "reason";
/// </summary>
public sealed class ResponseLoggingMiddleware
{
    /// <summary>Key used in HttpContext.Items for the response reason. Set this in controllers before returning.</summary>
    public const string ResponseReasonKey = "ResponseLogging.Reason";

    private readonly RequestDelegate _next;
    private readonly ILogger<ResponseLoggingMiddleware> _logger;

    public ResponseLoggingMiddleware(RequestDelegate next, ILogger<ResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        var statusCode = context.Response.StatusCode;
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;
        var reason = context.Items[ResponseReasonKey] as string;

        var reasonText = string.IsNullOrEmpty(reason) ? "" : $" - {reason}";
        var logMessage = "HTTP {Method} {Path} responded {StatusCode} ({StatusReason}){Reason} in {ElapsedMs}ms";

        if (statusCode >= 500)
            _logger.LogError(logMessage, method, path, statusCode, GetStatusReason(statusCode), reasonText, stopwatch.ElapsedMilliseconds);
        else if (statusCode >= 400)
            _logger.LogWarning(logMessage, method, path, statusCode, GetStatusReason(statusCode), reasonText, stopwatch.ElapsedMilliseconds);
        else
            _logger.LogInformation(logMessage, method, path, statusCode, GetStatusReason(statusCode), reasonText, stopwatch.ElapsedMilliseconds);
    }

    private static string GetStatusReason(int statusCode) => statusCode switch
    {
        200 => "OK",
        201 => "Created",
        400 => "Bad Request",
        401 => "Unauthorized",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        500 => "Internal Server Error",
        _ => statusCode.ToString()
    };
}
