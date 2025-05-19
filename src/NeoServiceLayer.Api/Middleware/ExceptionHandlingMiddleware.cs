using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Api.Models;
using NeoServiceLayer.Core.Exceptions;

namespace NeoServiceLayer.Api.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the ExceptionHandlingMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger.</param>
        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
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
                _logger.LogError(ex, "An unhandled exception occurred during request {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            try
            {
                context.Response.ContentType = "application/json";

                var (statusCode, errorCode, errorMessage, errorDetails) = MapExceptionToResponse(exception);
                context.Response.StatusCode = statusCode;

                // Create metadata with request ID if available
                var metadata = new ApiMetadata
                {
                    RequestId = context.TraceIdentifier
                };

                var response = ApiResponse<object>.CreateError(errorCode, errorMessage, errorDetails, metadata);
                var json = JsonSerializer.Serialize(response, _jsonOptions);

                // Check if the response has already started
                if (!context.Response.HasStarted)
                {
                    await context.Response.WriteAsync(json);
                }
            }
            catch (ObjectDisposedException)
            {
                // Stream is already closed, nothing to do
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during error handling
                _logger.LogError(ex, "Error occurred while handling an exception");
            }
        }

        private (int StatusCode, string ErrorCode, string ErrorMessage, object ErrorDetails) MapExceptionToResponse(Exception exception)
        {
            return exception switch
            {
                // 400 Bad Request
                ArgumentException _ => (
                    (int)HttpStatusCode.BadRequest,
                    ApiErrorCodes.BadRequest,
                    exception.Message,
                    null
                ),
                ValidationException validationEx => (
                    (int)HttpStatusCode.BadRequest,
                    ApiErrorCodes.ValidationError,
                    "Validation failed",
                    validationEx.Errors
                ),
                FormatException _ => (
                    (int)HttpStatusCode.BadRequest,
                    ApiErrorCodes.BadRequest,
                    exception.Message,
                    null
                ),

                // 401 Unauthorized
                UnauthorizedAccessException _ => (
                    (int)HttpStatusCode.Unauthorized,
                    ApiErrorCodes.Unauthorized,
                    "Authentication is required to access this resource",
                    null
                ),

                // 403 Forbidden
                ForbiddenException _ => (
                    (int)HttpStatusCode.Forbidden,
                    ApiErrorCodes.Forbidden,
                    "You do not have permission to access this resource",
                    null
                ),

                // 404 Not Found
                NotFoundException _ => (
                    (int)HttpStatusCode.NotFound,
                    ApiErrorCodes.NotFound,
                    exception.Message,
                    null
                ),
                KeyNotFoundException _ => (
                    (int)HttpStatusCode.NotFound,
                    ApiErrorCodes.NotFound,
                    exception.Message,
                    null
                ),

                // 409 Conflict
                ConflictException _ => (
                    (int)HttpStatusCode.Conflict,
                    ApiErrorCodes.Conflict,
                    exception.Message,
                    null
                ),

                // 429 Too Many Requests
                RateLimitExceededException _ => (
                    (int)HttpStatusCode.TooManyRequests,
                    ApiErrorCodes.RateLimited,
                    "Rate limit exceeded. Please try again later.",
                    null
                ),

                // 500 Internal Server Error
                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    ApiErrorCodes.InternalServerError,
                    "An unexpected error occurred. Please try again later.",
                    null
                )
            };
        }
    }
}
