using System;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NeoServiceLayer.Services.Permissions;
using NeoServiceLayer.Services.Permissions.Models;

namespace NeoServiceLayer.Web.Middleware;

/// <summary>
/// Middleware for enforcing permission checks on API endpoints.
/// Intercepts requests and validates user/service permissions before allowing access.
/// </summary>
public class PermissionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PermissionMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    public PermissionMiddleware(RequestDelegate next, ILogger<PermissionMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Invokes the middleware to check permissions for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Skip permission checks for certain paths
            if (ShouldSkipPermissionCheck(context))
            {
                await _next(context);
                return;
            }

            // Get permission service
            var permissionService = context.RequestServices.GetService<IPermissionService>();
            if (permissionService == null)
            {
                _logger.LogWarning("Permission service not available, allowing request");
                await _next(context);
                return;
            }

            // Extract user/service identity
            var identity = ExtractIdentity(context);
            if (identity == null)
            {
                await RejectRequest(context, "Authentication required", 401);
                return;
            }

            // Build resource and action from the request
            var resource = BuildResourceIdentifier(context);
            var action = context.Request.Method.ToUpperInvariant();

            // Check permissions
            var hasPermission = false;
            if (identity.Type == PrincipalType.Service)
            {
                var accessType = MapHttpMethodToAccessType(action);
                var result = await permissionService.CheckServicePermissionAsync(identity.Id, resource, accessType);
                hasPermission = result.IsAllowed;
                
                if (!hasPermission)
                {
                    _logger.LogWarning("Service {ServiceId} denied access to {Resource} with action {Action}: {Reason}",
                        identity.Id, resource, action, result.DenialReason);
                }
            }
            else
            {
                hasPermission = await permissionService.CheckPermissionAsync(identity.Id, resource, action);
                
                if (!hasPermission)
                {
                    _logger.LogWarning("User {UserId} denied access to {Resource} with action {Action}",
                        identity.Id, resource, action);
                }
            }

            if (!hasPermission)
            {
                await RejectRequest(context, "Insufficient permissions", 403);
                return;
            }

            // Add identity to context for downstream services
            context.Items["Principal"] = identity;
            
            // Continue with the request
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in permission middleware");
            await RejectRequest(context, "Permission check failed", 500);
        }
    }

    /// <summary>
    /// Determines if permission checks should be skipped for the current request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>True if permission checks should be skipped, false otherwise.</returns>
    private static bool ShouldSkipPermissionCheck(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        
        // Skip for health checks, swagger, and authentication endpoints
        return path.StartsWith("/health") ||
               path.StartsWith("/swagger") ||
               path.StartsWith("/api/auth") ||
               path.StartsWith("/api/permission/generate-token") ||
               path.StartsWith("/api/permission/validate-token") ||
               path == "/" ||
               path == "/favicon.ico" ||
               context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts identity information from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The extracted identity, or null if not found.</returns>
    private RequestIdentity? ExtractIdentity(HttpContext context)
    {
        // Check for service API key in headers
        if (context.Request.Headers.TryGetValue("X-Service-Key", out var serviceKey))
        {
            if (context.Request.Headers.TryGetValue("X-Service-Id", out var serviceId))
            {
                return new RequestIdentity
                {
                    Id = serviceId.ToString(),
                    Type = PrincipalType.Service,
                    AuthMethod = "service-key"
                };
            }
        }

        // Check for JWT token in Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            // Try to extract user ID from claims (assuming JWT is validated by auth middleware)
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                           user.FindFirst("sub")?.Value ??
                           user.FindFirst("user_id")?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    return new RequestIdentity
                    {
                        Id = userId,
                        Type = PrincipalType.User,
                        AuthMethod = "jwt",
                        Token = token
                    };
                }
            }
        }

        // Check for basic auth or other authentication methods
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.Identity.Name;
            if (!string.IsNullOrEmpty(userId))
            {
                return new RequestIdentity
                {
                    Id = userId,
                    Type = PrincipalType.User,
                    AuthMethod = "basic"
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Builds a resource identifier from the HTTP request.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The resource identifier.</returns>
    private static string BuildResourceIdentifier(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        
        // Remove API prefix and version if present
        if (path.StartsWith("/api/v"))
        {
            var versionEndIndex = path.IndexOf('/', 5);
            if (versionEndIndex > 0)
            {
                path = path.Substring(versionEndIndex);
            }
        }
        else if (path.StartsWith("/api/"))
        {
            path = path.Substring(4);
        }

        // Convert path to resource format (e.g., /users/123 -> users:123)
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return "api:root";
        }

        if (segments.Length == 1)
        {
            return $"api:{segments[0]}";
        }

        // Handle resource with ID (e.g., users/123 -> users:123)
        if (segments.Length == 2 && !segments[1].Contains('-'))
        {
            return $"api:{segments[0]}:{segments[1]}";
        }

        // Handle nested resources (e.g., users/123/roles -> users:123:roles)
        return $"api:{string.Join(":", segments)}";
    }

    /// <summary>
    /// Maps HTTP methods to access types.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <returns>The corresponding access type.</returns>
    private static AccessType MapHttpMethodToAccessType(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => AccessType.Read,
            "POST" => AccessType.Write,
            "PUT" => AccessType.Write,
            "PATCH" => AccessType.Write,
            "DELETE" => AccessType.Delete,
            _ => AccessType.Read
        };
    }

    /// <summary>
    /// Rejects the request with an appropriate error response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task RejectRequest(HttpContext context, string message, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = message,
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.Value,
            method = context.Request.Method
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }

    /// <summary>
    /// Represents the identity extracted from a request.
    /// </summary>
    private class RequestIdentity
    {
        /// <summary>
        /// Gets or sets the identity ID (user ID or service ID).
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the principal type (user or service).
        /// </summary>
        public PrincipalType Type { get; set; }

        /// <summary>
        /// Gets or sets the authentication method used.
        /// </summary>
        public string AuthMethod { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the authentication token (if applicable).
        /// </summary>
        public string? Token { get; set; }
    }
}

/// <summary>
/// Extension methods for adding permission middleware to the pipeline.
/// </summary>
public static class PermissionMiddlewareExtensions
{
    /// <summary>
    /// Adds the permission middleware to the request pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PermissionMiddleware>();
    }
}