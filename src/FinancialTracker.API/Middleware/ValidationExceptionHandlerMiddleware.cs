using FluentValidation;
using System.Net;
using System.Text.Json;

namespace FinancialTracker.API.Middleware;

public sealed class ValidationExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationExceptionHandlerMiddleware> _logger;

    public ValidationExceptionHandlerMiddleware(RequestDelegate next, ILogger<ValidationExceptionHandlerMiddleware> logger)
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
        catch (ValidationException ex)
        {
            var errorsSummary = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            _logger.LogWarning("Validation failed - {Errors} - {Path} {Method}", errorsSummary, context.Request.Path, context.Request.Method);
            await HandleValidationExceptionAsync(context, ex);
        }
    }

    private static Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";
        response.StatusCode = (int)HttpStatusCode.BadRequest;

        var errorsSummary = string.Join("; ", exception.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
        context.Items[ResponseLoggingMiddleware.ResponseReasonKey] = $"Validation failed: {errorsSummary}";

        var errorResponse = new ValidationErrorResponse
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            ErrorCode = "VALIDATION_ERROR",
            Message = "One or more validation errors occurred",
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path,
            Method = context.Request.Method,
            Errors = exception.Errors.Select(e => new ValidationError
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage,
                AttemptedValue = e.AttemptedValue?.ToString()
            }).ToList()
        };

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(errorResponse, jsonOptions);
        return response.WriteAsync(json);
    }
}

public sealed class ValidationErrorResponse : ErrorResponse
{
    public List<ValidationError> Errors { get; set; } = new();
}

public sealed class ValidationError
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? AttemptedValue { get; set; }
}
