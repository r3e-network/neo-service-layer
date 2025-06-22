using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
