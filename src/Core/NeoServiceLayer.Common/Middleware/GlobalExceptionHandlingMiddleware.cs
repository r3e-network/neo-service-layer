using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Common.Services;
using System.Net;
using System.Text.Json;

namespace NeoServiceLayer.Common.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly ICorrelationIdService _correlationIdService;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        ICorrelationIdService correlationIdService)
    {
        _next = next;
        _logger = logger;
        _correlationIdService = correlationIdService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = _correlationIdService.CorrelationId;
        
        _logger.LogError(exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}", 
            correlationId);

        var response = context.Response;
        response.ContentType = "application/json";

        var (statusCode, message) = GetErrorDetails(exception);
        response.StatusCode = (int)statusCode;

        var errorResponse = new ErrorResponse
        {
            Message = message,
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            Path = context.Request.Path.Value ?? string.Empty
        };

        // Include detailed error information in development
        if (IsDetailedErrorsEnabled(context))
        {
            errorResponse.Details = exception.ToString();
            errorResponse.StackTrace = exception.StackTrace;
        }

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await response.WriteAsync(jsonResponse);
    }

    private static (HttpStatusCode statusCode, string message) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid request parameters"),
            ArgumentNullException => (HttpStatusCode.BadRequest, "Required parameter is missing"),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            NotSupportedException => (HttpStatusCode.NotImplemented, "Operation not supported"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "Request timeout"),
            InvalidOperationException => (HttpStatusCode.Conflict, "Invalid operation"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };
    }

    private static bool IsDetailedErrorsEnabled(HttpContext context)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return environment?.Equals("Development", StringComparison.OrdinalIgnoreCase) == true;
    }
}

/// <summary>
/// Standard error response format
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Correlation ID for request tracing
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when error occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Request path where error occurred
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Detailed error information (development only)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Stack trace (development only)
    /// </summary>
    public string? StackTrace { get; set; }
}