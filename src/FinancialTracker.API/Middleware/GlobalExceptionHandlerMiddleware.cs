using System.Net;
using System.Text.Json;

namespace FinancialTracker.API.Middleware;

public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            Method = context.Request.Method
        };

        switch (exception)
        {
            case ArgumentException argEx:
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.ErrorCode = "INVALID_ARGUMENT";
                errorResponse.Message = argEx.Message;
                break;

            case InvalidOperationException opEx:
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.ErrorCode = "INVALID_OPERATION";
                errorResponse.Message = opEx.Message;
                break;

            case UnauthorizedAccessException:
                errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.ErrorCode = "UNAUTHORIZED";
                errorResponse.Message = "Unauthorized access";
                break;

            case KeyNotFoundException:
                errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.ErrorCode = "NOT_FOUND";
                errorResponse.Message = "Resource not found";
                break;

            default:
                errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.ErrorCode = "INTERNAL_ERROR";
                errorResponse.Message = "An internal server error occurred";
                break;
        }

        response.StatusCode = errorResponse.StatusCode;
        context.Items[ResponseLoggingMiddleware.ResponseReasonKey] = $"{errorResponse.ErrorCode}: {errorResponse.Message}";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(errorResponse, jsonOptions);
        return response.WriteAsync(json);
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
}
