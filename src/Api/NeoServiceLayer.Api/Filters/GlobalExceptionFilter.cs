using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NeoServiceLayer.Api.Controllers;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace NeoServiceLayer.Api.Filters;

/// <summary>
/// Global exception filter for handling unhandled exceptions across all controllers.
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionFilter"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="environment">The web host environment.</param>
    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <inheritdoc/>
    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var request = context.HttpContext.Request;
        var userId = context.HttpContext.User?.Identity?.Name ?? "Anonymous";

        // Log the exception with context
        _logger.LogError(exception, 
            "Unhandled exception occurred. User: {UserId}, Method: {Method}, Path: {Path}, Query: {Query}",
            userId, request.Method, request.Path, request.QueryString);

        // Create standardized error response
        var response = CreateErrorResponse(exception);
        
        // Set the HTTP status code
        context.HttpContext.Response.StatusCode = GetStatusCode(exception);
        
        // Set the result
        context.Result = new JsonResult(response)
        {
            StatusCode = context.HttpContext.Response.StatusCode
        };

        // Mark as handled
        context.ExceptionHandled = true;
    }

    /// <summary>
    /// Creates a standardized error response based on the exception type.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The error response.</returns>
    private ApiResponse<object> CreateErrorResponse(Exception exception)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Timestamp = DateTime.UtcNow
        };

        switch (exception)
        {
            case ValidationException validationEx:
                response.Message = "Validation failed";
                response.Errors = new Dictionary<string, string[]>
                {
                    ["validation"] = new[] { validationEx.Message }
                };
                break;

            case ArgumentException argumentEx:
                response.Message = "Invalid request parameters";
                response.Data = _environment.IsDevelopment() ? argumentEx.Message : null;
                break;

            case UnauthorizedAccessException:
                response.Message = "Access denied. Please check your credentials and permissions.";
                break;

            case NotSupportedException notSupportedEx:
                response.Message = "Operation not supported";
                response.Data = _environment.IsDevelopment() ? notSupportedEx.Message : null;
                break;

            case InvalidOperationException invalidOpEx:
                response.Message = "Invalid operation";
                response.Data = _environment.IsDevelopment() ? invalidOpEx.Message : null;
                break;

            case TimeoutException:
                response.Message = "The operation timed out. Please try again later.";
                break;

            case HttpRequestException httpEx:
                response.Message = "External service unavailable";
                response.Data = _environment.IsDevelopment() ? httpEx.Message : null;
                break;

            case TaskCanceledException:
                response.Message = "The operation was cancelled or timed out";
                break;

            case NotImplementedException:
                response.Message = "Feature not implemented";
                break;

            default:
                response.Message = "An internal server error occurred";
                response.Data = _environment.IsDevelopment() ? exception.Message : null;
                break;
        }

        // Add correlation ID for tracking
        response.Errors ??= new Dictionary<string, string[]>();
        response.Errors["correlationId"] = new[] { Guid.NewGuid().ToString() };

        return response;
    }

    /// <summary>
    /// Gets the appropriate HTTP status code based on the exception type.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The HTTP status code.</returns>
    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ValidationException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            NotSupportedException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            HttpRequestException => (int)HttpStatusCode.BadGateway,
            TaskCanceledException => (int)HttpStatusCode.RequestTimeout,
            NotImplementedException => (int)HttpStatusCode.NotImplemented,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }
}