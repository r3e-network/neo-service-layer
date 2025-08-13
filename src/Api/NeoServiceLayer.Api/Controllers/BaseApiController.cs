using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Core;

namespace NeoServiceLayer.Api.Controllers;

/// <summary>
/// Base controller for all Neo Service Layer API controllers.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Gets the logger for the controller.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseApiController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    protected BaseApiController(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Gets the current user ID from the JWT token.
    /// </summary>
    protected string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Gets the current user's roles from the JWT token.
    /// </summary>
    protected IEnumerable<string> GetCurrentUserRoles()
    {
        return User.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    /// <summary>
    /// Gets the current user ID (alias for GetCurrentUserId).
    /// </summary>
    protected string? GetUserId()
    {
        return GetCurrentUserId() ?? HttpContext.Items["UserId"]?.ToString();
    }

    /// <summary>
    /// Gets the current session ID from claims or context.
    /// </summary>
    protected string? GetSessionId()
    {
        return User?.FindFirst("session_id")?.Value ?? HttpContext.Items["SessionId"]?.ToString();
    }

    /// <summary>
    /// Gets the access token from the request.
    /// </summary>
    protected string? GetAccessToken()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring(7);
        }
        return HttpContext.Items["Token"]?.ToString();
    }

    /// <summary>
    /// Gets the client IP address.
    /// </summary>
    protected string GetClientIpAddress()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        // Check for forwarded headers (when behind proxy/load balancer)
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }
        else if (Request.Headers.ContainsKey("X-Real-IP"))
        {
            ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        
        return ipAddress ?? "Unknown";
    }

    /// <summary>
    /// Gets the user agent string.
    /// </summary>
    protected string GetUserAgent()
    {
        return Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
    }

    /// <summary>
    /// Checks if the current user has a specific role.
    /// </summary>
    protected bool HasRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }

    /// <summary>
    /// Checks if the current user has any of the specified roles.
    /// </summary>
    protected bool HasAnyRole(params string[] roles)
    {
        return roles?.Any(role => User?.IsInRole(role) ?? false) ?? false;
    }

    /// <summary>
    /// Gets the correlation ID for request tracking.
    /// </summary>
    protected string GetCorrelationId()
    {
        return HttpContext.Items["CorrelationId"]?.ToString() 
            ?? Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Creates a standardized API response.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="data">The response data.</param>
    /// <param name="message">Optional message.</param>
    /// <param name="success">Whether the operation was successful.</param>
    /// <returns>The API response.</returns>
    protected ApiResponse<T> CreateResponse<T>(T data, string? message = null, bool success = true)
    {
        return new ApiResponse<T>
        {
            Success = success,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a standardized error response.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="details">Optional error details.</param>
    /// <returns>The error response.</returns>
    protected ApiResponse<object> CreateErrorResponse(string message, object? details = null)
    {
        return new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = details,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Handles exceptions and returns appropriate error responses.
    /// </summary>
    /// <param name="ex">The exception.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <returns>The error response.</returns>
    protected IActionResult HandleException(Exception ex, string operation)
    {
        Logger.LogError(ex, "Error during {Operation}", operation);

        return ex switch
        {
            ArgumentException => BadRequest(CreateErrorResponse(ex.Message)),
            UnauthorizedAccessException => Unauthorized(CreateErrorResponse("Access denied")),
            NotSupportedException => BadRequest(CreateErrorResponse(ex.Message)),
            InvalidOperationException => BadRequest(CreateErrorResponse(ex.Message)),
            _ => StatusCode(500, CreateErrorResponse("An internal error occurred",
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ? ex.Message : null))
        };
    }

    /// <summary>
    /// Validates the blockchain type parameter.
    /// </summary>
    /// <param name="blockchainType">The blockchain type.</param>
    /// <returns>True if valid, false otherwise.</returns>
    protected bool IsValidBlockchainType(string blockchainType)
    {
        return Enum.TryParse<BlockchainType>(blockchainType, true, out _);
    }

    /// <summary>
    /// Parses the blockchain type from string.
    /// </summary>
    /// <param name="blockchainType">The blockchain type string.</param>
    /// <returns>The parsed blockchain type.</returns>
    protected BlockchainType ParseBlockchainType(string blockchainType)
    {
        if (!Enum.TryParse<BlockchainType>(blockchainType, true, out var result))
        {
            throw new ArgumentException($"Invalid blockchain type: {blockchainType}");
        }
        return result;
    }
}

/// <summary>
/// Standardized API response format.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets whether the operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the response data.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the response message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the response timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets any validation errors.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }
}

/// <summary>
/// Paginated response format.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public class PaginatedResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Gets whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
