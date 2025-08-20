using System.Net;
using System.Text.Json;
using NeoServiceLayer.Api.Controllers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;


namespace NeoServiceLayer.Api.Middleware;

/// <summary>
/// Middleware for handling exceptions that occur outside of MVC pipeline.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="environment">The web host environment.</param>
    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Handles exceptions that occur in the middleware pipeline.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = Guid.NewGuid().ToString();
        var userId = context.User?.Identity?.Name ?? "Anonymous";

        // Log the exception with full context
        _logger.LogError(exception,
            "Unhandled middleware exception. CorrelationId: {CorrelationId}, User: {UserId}, Method: {Method}, Path: {Path}, Query: {Query}, RemoteIP: {RemoteIP}",
            correlationId, userId, context.Request.Method, context.Request.Path,
            context.Request.QueryString, context.Connection.RemoteIpAddress);

        // Ensure response hasn't started
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Cannot modify response, it has already started. CorrelationId: {CorrelationId}", correlationId);
            return;
        }

        // Create standardized error response
        var response = CreateErrorResponse(exception, correlationId);
        var statusCode = GetStatusCode(exception);

        // Set response properties
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Serialize and write response
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Creates a standardized error response.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <param name="correlationId">The correlation ID for tracking.</param>
    /// <returns>The error response.</returns>
    private ApiResponse<object> CreateErrorResponse(Exception exception, string correlationId)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Timestamp = DateTime.UtcNow,
            Errors = new Dictionary<string, string[]>
            {
                ["correlationId"] = new[] { correlationId }
            }
        };

        // Set message and details based on exception type
        switch (exception)
        {
            case ValidationException validationEx:
                response.Message = "Request validation failed";
                response.Errors["validation"] = new[] { validationEx.Message };
                break;

            case ArgumentException argumentEx:
                response.Message = "Invalid request parameters";
                if (_environment.IsDevelopment())
                {
                    response.Data = argumentEx.Message;
                    response.Errors["parameter"] = new[] { argumentEx.ParamName ?? "unknown" };
                }
                break;

            case UnauthorizedAccessException:
                response.Message = "Authentication required or access denied";
                break;

            case InvalidOperationException invalidOpEx:
                response.Message = "The requested operation cannot be performed";
                if (_environment.IsDevelopment())
                {
                    response.Data = invalidOpEx.Message;
                }
                break;

            case NotSupportedException notSupportedEx:
                response.Message = "The requested operation is not supported";
                if (_environment.IsDevelopment())
                {
                    response.Data = notSupportedEx.Message;
                }
                break;

            case TimeoutException:
                response.Message = "The operation timed out. Please try again later.";
                break;

            case HttpRequestException httpEx:
                response.Message = "External service communication failed";
                if (_environment.IsDevelopment())
                {
                    response.Data = httpEx.Message;
                }
                break;

            case TaskCanceledException:
                response.Message = "The operation was cancelled or timed out";
                break;

            case OutOfMemoryException:
                response.Message = "Insufficient resources to complete the operation";
                break;

            case StackOverflowException:
                response.Message = "The operation caused a stack overflow";
                break;

            default:
                response.Message = "An unexpected error occurred";
                if (_environment.IsDevelopment())
                {
                    response.Data = new
                    {
                        type = exception.GetType().Name,
                        message = exception.Message,
                        stackTrace = exception.StackTrace?.Split('\n').Take(10) // Limit stack trace in dev
                    };
                }
                break;
        }

        return response;
    }

    /// <summary>
    /// Gets the appropriate HTTP status code for the exception.
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
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            NotSupportedException => (int)HttpStatusCode.BadRequest,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            HttpRequestException => (int)HttpStatusCode.BadGateway,
            TaskCanceledException => (int)HttpStatusCode.RequestTimeout,
            OutOfMemoryException => (int)HttpStatusCode.InsufficientStorage,
            StackOverflowException => (int)HttpStatusCode.InternalServerError,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }
}
